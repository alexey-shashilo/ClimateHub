using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using ClimateHub.Modules.Commands.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClimateHub.Modules.Commands.Infrastructure;

public class CommandTimeoutOptions
{
    public const string SectionName = "CommandTimeout";

    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan QueuedTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int InitialDelayMs { get; set; } = 10000;
}

public class CommandTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CommandTimeoutOptions> _options;
    private readonly ILogger<CommandTimeoutWorker> _logger;

    public CommandTimeoutWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CommandTimeoutOptions> options,
        ILogger<CommandTimeoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommandTimeoutWorker started");
        await Task.Delay(_options.Value.InitialDelayMs, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cmdRepo = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
                var opts = _options.Value;

                var staleness = DateTimeOffset.UtcNow.Add(-opts.ExecutionTimeout);
                var staleCommands = await cmdRepo.GetStaleAsync(staleness, stoppingToken);

                foreach (var cmd in staleCommands)
                {
                    try
                    {
                        cmd.Timeout();
                        await cmdRepo.UpdateAsync(cmd, stoppingToken);
                        _logger.LogInformation("Command {Id} timed out (execution > {Timeout})", cmd.Id, opts.ExecutionTimeout);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to timeout command {Id}", cmd.Id);
                    }
                }

                var queuedCutoff = DateTimeOffset.UtcNow.Add(-opts.QueuedTimeout);
                var stuckQueued = await cmdRepo.GetStuckQueuedAsync(queuedCutoff, stoppingToken);
                foreach (var cmd in stuckQueued)
                {
                    try
                    {
                        cmd.Timeout();
                        await cmdRepo.UpdateAsync(cmd, stoppingToken);
                        _logger.LogInformation("Stuck queued command {Id} timed out (queued > {Timeout})", cmd.Id, opts.QueuedTimeout);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to timeout stuck queued command {Id}", cmd.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandTimeoutWorker error");
            }

            await Task.Delay(_options.Value.ScanInterval, stoppingToken);
        }
    }
}
