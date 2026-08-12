using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.Modules.Environment.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Environment.Infrastructure;

public class TelemetryOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelemetryOutboxWorker> _logger;
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

    public TelemetryOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<TelemetryOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelemetryOutboxWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outboxRepo = scope.ServiceProvider.GetRequiredService<ITelemetryOutboxRepository>();
                var influxWriter = scope.ServiceProvider.GetRequiredService<IInfluxDbWriter>();

                // Recover stuck Processing records
                await outboxRepo.RecoverStuckAsync(ProcessingTimeout, stoppingToken);

                var pending = await outboxRepo.GetPendingAsync(BatchSize, stoppingToken);

                foreach (var item in pending)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        // Mark as Processing with lease
                        var acquired = await outboxRepo.TryAcquireAsync(item.Id, stoppingToken);
                        if (!acquired) continue;

                        await influxWriter.WriteMeasurementAsync(
                            item.RoomId, item.DeviceId, item.MeasuredAt,
                            item.TemperatureC, item.RelativeHumidityPct, item.Co2Ppm,
                            item.Quality, stoppingToken);

                        await outboxRepo.MarkProcessedAsync(item.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "TelemetryOutbox flush failed for item {Id}, attempt {Attempt}",
                            item.Id, item.AttemptCount);

                        var isRetryable = IsRetryable(ex);
                        if (isRetryable)
                        {
                            var delay = CalculateRetryDelay(item.AttemptCount);
                            await outboxRepo.MarkRetryableAsync(item.Id, ex.Message, delay, stoppingToken);
                        }
                        else
                        {
                            await outboxRepo.MarkFailedAsync(item.Id, ex.Message, stoppingToken);
                            _logger.LogError("TelemetryOutbox permanent failure for item {Id}: {Error}", item.Id, ex.Message);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TelemetryOutboxWorker cycle error");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("TelemetryOutboxWorker stopped");
    }

    private static bool IsRetryable(Exception ex)
    {
        var msg = ex.Message;
        return msg.Contains("connect", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("refused", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("429", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("5", StringComparison.OrdinalIgnoreCase);
    }

    internal static TimeSpan CalculateRetryDelay(int attemptCount)
    {
        var baseDelay = attemptCount switch
        {
            0 => TimeSpan.FromSeconds(2),
            1 => TimeSpan.FromSeconds(5),
            2 => TimeSpan.FromSeconds(10),
            3 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromMinutes(1),
        };

        var jitter = Random.Shared.NextDouble() * 0.3 + 0.85;
        var total = TimeSpan.FromTicks((long)(baseDelay.Ticks * jitter));
        return total > MaxRetryDelay ? MaxRetryDelay : total;
    }
}
