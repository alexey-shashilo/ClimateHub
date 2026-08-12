using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClimateHub.Infrastructure.InternalEvents;

public class InternalEventsOptions
{
    public const string SectionName = "InternalEvents";

    public int BatchSize { get; set; } = 50;
    public int PollingIntervalMs { get; set; } = 1000;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(2);
    public int MaximumAttempts { get; set; } = 5;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaximumRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public double JitterFactor { get; set; } = 0.2;
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan ProcessedRetention { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan FailedRetention { get; set; } = TimeSpan.FromDays(30);
}

public delegate Task InternalEventHandler(InternalEventEnvelope envelope, CancellationToken ct);

public class InternalEventDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly InternalEventOutboxRepository _outboxRepo;
    private readonly InternalEventInboxRepository _inboxRepo;
    private readonly SseEventLogRepository _sseRepo;
    private readonly IOptions<InternalEventsOptions> _options;
    private readonly ILogger<InternalEventDispatcher> _logger;
    private readonly Dictionary<string, List<InternalEventHandler>> _handlers = new();

    public InternalEventDispatcher(
        IServiceScopeFactory scopeFactory,
        InternalEventOutboxRepository outboxRepo,
        InternalEventInboxRepository inboxRepo,
        SseEventLogRepository sseRepo,
        IOptions<InternalEventsOptions> options,
        ILogger<InternalEventDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _outboxRepo = outboxRepo;
        _inboxRepo = inboxRepo;
        _sseRepo = sseRepo;
        _options = options;
        _logger = logger;
    }

    public void Subscribe(string eventType, InternalEventHandler handler)
    {
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<InternalEventHandler>();
        _handlers[eventType].Add(handler);
    }

    public async Task DispatchAsync(InternalEventEnvelope envelope, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(envelope.EventType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for event type {EventType}", envelope.EventType);
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await handler(envelope with { }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler failed for event {EventId} type {EventType}", envelope.EventId, envelope.EventType);
                throw;
            }
        }
    }
}

public class InternalEventOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<InternalEventsOptions> _options;
    private readonly ILogger<InternalEventOutboxWorker> _logger;

    public InternalEventOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<InternalEventsOptions> options,
        ILogger<InternalEventOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InternalEventOutboxWorker started");
        await Task.Delay(3000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outboxRepo = scope.ServiceProvider.GetRequiredService<InternalEventOutboxRepository>();
                var inboxRepo = scope.ServiceProvider.GetRequiredService<InternalEventInboxRepository>();
                var sseRepo = scope.ServiceProvider.GetRequiredService<SseEventLogRepository>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<InternalEventDispatcher>();
                var opts = _options.Value;

                await outboxRepo.RecoverStuckProcessingAsync(opts.ProcessingTimeout, stoppingToken);

                var pending = await outboxRepo.GetPendingAsync(opts.BatchSize, stoppingToken);

                foreach (var row in pending)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    if (row.AttemptCount >= opts.MaximumAttempts)
                    {
                        await outboxRepo.MarkFailedAsync(row.Id, "MAX_ATTEMPTS_EXCEEDED", stoppingToken);
                        continue;
                    }

                    var acquired = await outboxRepo.TryAcquireAsync(row.Id, stoppingToken);
                    if (!acquired) continue;

                    try
                    {
                        var envelope = new InternalEventEnvelope(
                            row.EventId, row.EventType, row.AggregateType,
                            row.AggregateId, row.BuildingId, row.RoomId,
                            row.OccurredAt, row.Payload, row.Headers,
                            row.CorrelationId, row.CausationId);

                        await dispatcher.DispatchAsync(envelope, stoppingToken);

                        // Append to SSE event log
                        if (row.EventType.StartsWith("need.") || row.EventType.StartsWith("climate.")
                            || row.EventType.StartsWith("engineering.command-plan."))
                        {
                            await sseRepo.AppendAsync(
                                row.EventId, row.EventType,
                                row.BuildingId, row.RoomId,
                                row.Payload, row.OccurredAt, stoppingToken);
                        }

                        await outboxRepo.MarkProcessedAsync(row.Id, stoppingToken);
                        _logger.LogDebug("Dispatched event {EventId} type {Type}", row.EventId, row.EventType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to dispatch event {EventId} type {Type} attempt {Attempt}",
                            row.EventId, row.EventType, row.AttemptCount);

                        var delay = CalculateRetryDelay(row.AttemptCount, opts);
                        await outboxRepo.MarkRetryableAsync(row.Id, ex.GetType().Name, delay, stoppingToken);
                    }
                }

                // Periodic cleanup
                var random = Random.Shared.Next(0, 20);
                if (random == 0)
                {
                    await outboxRepo.CleanupProcessedAsync(opts.ProcessedRetention, stoppingToken);
                    await outboxRepo.CleanupFailedAsync(opts.FailedRetention, stoppingToken);
                    await inboxRepo.CleanupProcessedAsync(opts.ProcessedRetention, stoppingToken);
                    await sseRepo.CleanupAsync(opts.ProcessedRetention, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InternalEventOutboxWorker cycle error");
            }

            await Task.Delay(_options.Value.PollingIntervalMs, stoppingToken);
        }
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount, InternalEventsOptions opts)
    {
        var baseDelay = opts.InitialRetryDelay;
        for (int i = 1; i < attemptCount; i++)
            baseDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * 2);

        baseDelay = TimeSpan.FromMilliseconds(Math.Min(baseDelay.TotalMilliseconds, opts.MaximumRetryDelay.TotalMilliseconds));

        var jitter = baseDelay.TotalMilliseconds * opts.JitterFactor * (Random.Shared.NextDouble() * 2 - 1);
        return TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds + jitter);
    }
}
