using ClimateHub.Modules.Climate.Application.EffectModel;
using ClimateHub.Modules.Climate.Application.Events;
using ClimateHub.Modules.Climate.Domain;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.Climate.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClimateHub.Modules.Climate.Application.Workers;

public class ClimateReconciliationOptions
{
    public bool Enabled { get; set; } = true;
    public int ScanIntervalMs { get; set; } = 15000;
    public int BatchSize { get; set; } = 20;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan EffectEvaluationDelay { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan MaximumEffectWaitDuration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan SubPlanStaleThreshold { get; set; } = TimeSpan.FromMinutes(10);
    public int MaximumAttempts { get; set; } = 3;
}

public class ClimateReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ClimateReconciliationOptions> _options;
    private readonly ILogger<ClimateReconciliationWorker> _logger;

    public ClimateReconciliationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ClimateReconciliationOptions> options,
        ILogger<ClimateReconciliationWorker> logger)
    {
        _scopeFactory = scopeFactory; _options = options; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ClimateReconciliationWorker started");
        await Task.Delay(10000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_options.Value.Enabled)
                {
                    await Task.Delay(30000, stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                await ReconcileAsync(scope, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClimateReconciliationWorker scan error");
            }

            await Task.Delay(_options.Value.ScanIntervalMs, stoppingToken);
        }
    }

    private async Task ReconcileAsync(IServiceScope scope, CancellationToken ct)
    {
        var planRepo = scope.ServiceProvider.GetRequiredService<IClimatePlanRepository>();
        var goalRepo = scope.ServiceProvider.GetRequiredService<IClimateGoalRepository>();
        var resourceRepo = scope.ServiceProvider.GetRequiredService<IClimateResourceRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<ClimateLifecycleEventPublisher>();
        var effectEvaluator = scope.ServiceProvider.GetRequiredService<ClimatePlanEffectEvaluator>();
        var opts = _options.Value;

        await effectEvaluator.EvaluateEffectAsync(ct);

        var duePlans = await planRepo.GetDueForEffectAsync(opts.MaximumEffectWaitDuration, ct);
        foreach (var plan in duePlans)
        {
            if (plan.Status == ClimatePlanStatus.WaitingForEffect && plan.WaitingForEffectAt.HasValue)
            {
                var elapsed = DateTimeOffset.UtcNow - plan.WaitingForEffectAt.Value;
                if (elapsed >= opts.MaximumEffectWaitDuration)
                {
                    _logger.LogWarning("Effect timeout for plan {PlanId} after {Elapsed}", plan.Id, elapsed);
                    plan.Fail(ClimateErrors.EffectNotObserved, $"No environmental effect observed within {opts.MaximumEffectWaitDuration.TotalMinutes} minutes");
                    await planRepo.UpdateAsync(plan, ct);
                    await eventPublisher.PublishPlanEventAsync("climate.effect.timeout", plan, ct: ct);
                    await eventPublisher.PublishPlanEventAsync("climate.plan.failed", plan, ct: ct);

                    await ReleasePlanResources(plan, resourceRepo, ct);

                    var goal = await goalRepo.GetByIdAsync(plan.GoalId, ct);
                    if (goal is not null && goal.Status != GoalStatus.Satisfied && goal.Status != GoalStatus.Disabled)
                    {
                        goal.Block();
                        await goalRepo.UpdateAsync(goal, ct);
                        await eventPublisher.PublishGoalEventAsync("climate.goal.blocked", goal, ct: ct);
                    }
                }
            }
        }

        var activePlans = await planRepo.GetActivePlansAsync(ct);
        foreach (var plan in activePlans)
        {
            foreach (var subPlan in plan.SubPlans)
            {
                if (subPlan.Status == SubPlanStatus.Executing && subPlan.StartedAt.HasValue)
                {
                    var elapsed = DateTimeOffset.UtcNow - subPlan.StartedAt.Value;
                    if (elapsed > opts.SubPlanStaleThreshold && string.IsNullOrEmpty(subPlan.EngineeringCommandPlanId))
                    {
                        _logger.LogWarning("Stale sub-plan {SubPlanId} in plan {PlanId}", subPlan.Id, plan.Id);
                        subPlan.MarkEngineeringFailed("STALE_SUB_PLAN");
                        await planRepo.UpdateAsync(plan, ct);
                    }
                }
            }
        }

        var activeGoals = await goalRepo.GetActiveAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var goal in activeGoals)
        {
            if (goal.LastEvaluatedAt is null || (now - goal.LastEvaluatedAt.Value).TotalMinutes > 30)
            {
                _logger.LogDebug("Goal {GoalId} for room {RoomId} due for evaluation", goal.Id, goal.RoomId);
            }
        }
    }

    private async Task ReleasePlanResources(ClimatePlan plan, IClimateResourceRepository resourceRepo, CancellationToken ct)
    {
        foreach (var reservation in plan.ResourceReservations)
        {
            if (reservation.Status == "Reserved" || reservation.Status == "Allocated")
            {
                var resource = await resourceRepo.GetByCodeAsync(
                    plan.BuildingId,
                    reservation.ClimateResourceId.ToString(),
                    ct);
                if (resource is not null)
                {
                    resource.Release(reservation.ReservedAmount);
                    await resourceRepo.UpdateAsync(resource, ct);
                }
                reservation.Release();
            }
        }
    }
}