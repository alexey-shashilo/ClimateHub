using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClimateHub.Modules.EngineeringSystems.Application.Thermal;

public class ThermalReconciliationOptions
{
    public bool Enabled { get; set; } = true;
    public int ScanIntervalMs { get; set; } = 30000;
    public int BatchSize { get; set; } = 10;
    public TimeSpan SourceStateStaleThreshold { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan PumpOverrunDefault { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan EffectEvaluationDelay { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaximumEffectWait { get; set; } = TimeSpan.FromMinutes(60);
}

public class ThermalReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ThermalReconciliationOptions> _options;
    private readonly ILogger<ThermalReconciliationWorker> _logger;

    public ThermalReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ThermalReconciliationOptions> options,
        ILogger<ThermalReconciliationWorker> logger)
    {
        _scopeFactory = scopeFactory; _options = options; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ThermalReconciliationWorker started");
        await Task.Delay(15000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.Value.Enabled) { await Task.Delay(30000, stoppingToken); continue; }
                using var scope = _scopeFactory.CreateScope();
                await ReconcileAsync(scope, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Reconciliation scan error"); }
            await Task.Delay(_options.Value.ScanIntervalMs, stoppingToken);
        }
    }

    private async Task ReconcileAsync(IServiceScope scope, CancellationToken ct)
    {
        var planRepo = scope.ServiceProvider.GetRequiredService<ICommandPlanRepository>();
        var activePlans = await planRepo.GetActiveAsync(ct);

        foreach (var plan in activePlans)
        {
            if (plan.NeedType == "eng.temperature.increase" || plan.NeedType == "eng.temperature.decrease")
            {
                if (plan.Status == CommandPlanStatus.WaitingForCommands && plan.CompletedAt is null)
                {
                    _logger.LogWarning("Stale thermal plan {PlanId}", plan.Id);
                }
            }
        }

        _logger.LogDebug("Thermal reconciliation scan completed: {Count} active plans", activePlans.Count);
    }
}
