using System.Diagnostics;
using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Modules.Climate.Application.Events;
using ClimateHub.Modules.Climate.Domain;
using ClimateHub.Modules.Climate.Domain.Repositories;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClimateHub.Modules.Climate.Application.Workers;

public class ClimateEventConsumerOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int PollingIntervalMs { get; set; } = 1000;
    public int MaximumAttempts { get; set; } = 5;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan InboxProcessingTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class ClimateEventConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ClimateEventConsumerOptions> _options;
    private readonly ILogger<ClimateEventConsumerWorker> _logger;

    public ClimateEventConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ClimateEventConsumerOptions> options,
        ILogger<ClimateEventConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory; _options = options; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClimateEventConsumerWorker started");
        await Task.Delay(5000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.Value.Enabled)
                {
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var opts = _options.Value;
                var outboxRepo = scope.ServiceProvider.GetRequiredService<InternalEventOutboxRepository>();
                var sseProjection = scope.ServiceProvider.GetRequiredService<ClimateSseProjectionHandler>();

                await outboxRepo.RecoverStuckProcessingAsync(opts.InboxProcessingTimeout, stoppingToken);

                var pending = await outboxRepo.GetPendingAsync(opts.BatchSize, stoppingToken);
                var climateEvents = pending.Where(e => IsClimateEvent(e.EventType)).ToList();

                foreach (var row in climateEvents)
                {
                    if (stoppingToken.IsCancellationRequested) break;
                    await ProcessEventAsync(row, scope, opts, sseProjection, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClimateEventConsumerWorker cycle error");
            }

            await Task.Delay(_options.Value.PollingIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessEventAsync(
        InternalEventOutbox row, IServiceScope scope,
        ClimateEventConsumerOptions opts,
        ClimateSseProjectionHandler sseProjection,
        CancellationToken ct)
    {
        if (row.AttemptCount >= opts.MaximumAttempts)
        {
            await MarkOutboxFailedAsync(scope, row.Id, "MAX_ATTEMPTS_EXCEEDED", ct);
            return;
        }

        var acquired = await TryAcquireOutboxAsync(scope, row.Id, ct);
        if (!acquired) return;

        var activity = new Activity("climate.event.consume");
        activity.Start();

        try
        {
            var climateInbox = scope.ServiceProvider.GetRequiredService<IClimateEventInboxRepository>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<InternalEventDispatcher>();

            var inboxEntry = new ClimateEventInboxEntry("climate-consumer",
                row.EventId.ToString(), row.EventType,
                row.AggregateId, row.BuildingId?.ToString(), row.RoomId?.ToString());
            await climateInbox.AddAsync(inboxEntry, ct);
            inboxEntry.StartProcessing();

            var envelope = new InternalEventEnvelope(
                row.EventId, row.EventType, row.AggregateType,
                row.AggregateId, row.BuildingId, row.RoomId,
                row.OccurredAt, row.Payload, row.Headers,
                row.CorrelationId, row.CausationId);
            await dispatcher.DispatchAsync(envelope, ct);

            inboxEntry.Processed();
            await climateInbox.UpdateAsync(inboxEntry, ct);

            await sseProjection.ProjectAsync(row, ct);

            await MarkOutboxProcessedAsync(scope, row.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Climate event {EventId} type {Type} failed attempt {Attempt}",
                row.EventId, row.EventType, row.AttemptCount);
            var delay = CalculateRetryDelay(row.AttemptCount);
            await MarkOutboxRetryableAsync(scope, row.Id, ex.GetType().Name, delay, ct);
        }
        finally
        {
            activity.Stop();
        }
    }

    private static bool IsClimateEvent(string eventType) => eventType switch
    {
        "need.detected" or "need.updated" or "need.satisfied" or "need.cancelled" => true,
        "environment.state.changed" => true,
        "room.policy.changed" => true,
        "engineering.command-plan.created" or "engineering.command-plan.executing" => true,
        "engineering.command-plan.succeeded" or "engineering.command-plan.failed" => true,
        "engineering.command-plan.cancelled" or "engineering.command-plan.expired" => true,
        "climate.strategy.changed" or "climate.resource.changed" => true,
        _ => false
    };

    private async Task<bool> TryAcquireOutboxAsync(IServiceScope scope, long outboxId, CancellationToken ct)
    {
        var repo = scope.ServiceProvider.GetRequiredService<InternalEventOutboxRepository>();
        return await repo.TryAcquireAsync(outboxId, ct);
    }

    private async Task MarkOutboxProcessedAsync(IServiceScope scope, long outboxId, CancellationToken ct)
    {
        var repo = scope.ServiceProvider.GetRequiredService<InternalEventOutboxRepository>();
        await repo.MarkProcessedAsync(outboxId, ct);
    }

    private async Task MarkOutboxFailedAsync(IServiceScope scope, long outboxId, string failureCode, CancellationToken ct)
    {
        var repo = scope.ServiceProvider.GetRequiredService<InternalEventOutboxRepository>();
        await repo.MarkFailedAsync(outboxId, failureCode, ct);
    }

    private async Task MarkOutboxRetryableAsync(IServiceScope scope, long outboxId, string failureCode, TimeSpan delay, CancellationToken ct)
    {
        var repo = scope.ServiceProvider.GetRequiredService<InternalEventOutboxRepository>();
        await repo.MarkRetryableAsync(outboxId, failureCode, delay, ct);
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (int i = 1; i < attemptCount; i++)
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
        return TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds, TimeSpan.FromMinutes(5).TotalMilliseconds));
    }
}

