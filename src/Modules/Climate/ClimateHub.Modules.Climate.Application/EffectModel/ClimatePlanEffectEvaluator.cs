using ClimateHub.Modules.Climate.Application.Events;
using ClimateHub.Modules.Climate.Domain;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.Climate.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Climate.Application.EffectModel;

public class ClimatePlanEffectEvaluator
{
    private readonly IClimatePlanRepository _planRepo;
    private readonly IClimateGoalRepository _goalRepo;
    private readonly IClimateResourceRepository _resourceRepo;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly ClimateLifecycleEventPublisher _eventPublisher;
    private readonly ILogger<ClimatePlanEffectEvaluator> _logger;

    public ClimatePlanEffectEvaluator(
        IClimatePlanRepository planRepo,
        IClimateGoalRepository goalRepo,
        IClimateResourceRepository resourceRepo,
        IRoomEnvironmentStateReader envReader,
        ClimateLifecycleEventPublisher eventPublisher,
        ILogger<ClimatePlanEffectEvaluator> logger)
    {
        _planRepo = planRepo;
        _goalRepo = goalRepo;
        _resourceRepo = resourceRepo;
        _envReader = envReader;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task EvaluateEffectAsync(CancellationToken ct = default)
    {
        var waitingPlans = await _planRepo.GetByStatusAsync(ClimatePlanStatus.WaitingForEffect, ct);
        foreach (var plan in waitingPlans)
        {
            await EvaluatePlanEffectAsync(plan, ct);
        }
    }

    public async Task EvaluatePlanEffectAsync(ClimatePlan plan, CancellationToken ct = default)
    {
        if (plan.Status != ClimatePlanStatus.WaitingForEffect)
            return;

        var goal = await _goalRepo.GetByIdAsync(plan.GoalId, ct);
        if (goal is null)
        {
            _logger.LogWarning("Goal {GoalId} not found for plan {PlanId}", plan.GoalId, plan.Id);
            return;
        }

        var parameters = await _envReader.GetParametersAsync(goal.RoomId, ct);
        if (parameters.Count == 0)
        {
            _logger.LogDebug("No environment data for room {RoomId}, deferring effect evaluation", goal.RoomId);
            return;
        }

        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        var humParam = parameters.FirstOrDefault(p => p.Parameter == "humidity");
        var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");
        var illParam = parameters.FirstOrDefault(p => p.Parameter == "illuminance");

        goal.UpdateCurrentConditions(
            tempParam?.Value, humParam?.Value, co2Param?.Value, illParam?.Value);

        bool satisfied = IsGoalSatisfied(goal);

        if (satisfied)
        {
            goal.MarkSatisfied();
            plan.Complete();

            await _goalRepo.UpdateAsync(goal, ct);
            await _planRepo.UpdateAsync(plan, ct);
            await ReleaseResourcesAsync(plan, ct);

            await _eventPublisher.PublishGoalEventAsync("climate.goal.satisfied", goal, ct: ct);
            await _eventPublisher.PublishPlanEventAsync("climate.plan.completed", plan, ct: ct);
            _logger.LogInformation("Goal {GoalId} and Plan {PlanId} completed based on environment measurement",
                goal.Id, plan.Id);
        }
        else
        {
            await _goalRepo.UpdateAsync(goal, ct);
            _logger.LogDebug("Goal {GoalId} not yet satisfied by current environment", goal.Id);
        }
    }

    private static bool IsGoalSatisfied(ClimateGoal goal)
    {
        bool satisfied = true;

        if (goal.CurrentCo2.HasValue && goal.CurrentCo2.Value > goal.TargetCo2Max + 50)
            satisfied = false;

        if (goal.CurrentTemperature.HasValue)
        {
            if (goal.CurrentTemperature.Value < goal.TargetTemperatureMin - 0.5 ||
                goal.CurrentTemperature.Value > goal.TargetTemperatureMax + 0.5)
                satisfied = false;
        }

        if (goal.CurrentHumidity.HasValue)
        {
            if (goal.CurrentHumidity.Value < goal.TargetHumidityMin - 2 ||
                goal.CurrentHumidity.Value > goal.TargetHumidityMax + 2)
                satisfied = false;
        }

        return satisfied;
    }

    private async Task ReleaseResourcesAsync(ClimatePlan plan, CancellationToken ct)
    {
        foreach (var reservation in plan.ResourceReservations)
        {
            if (reservation.Status is "Reserved" or "Allocated")
            {
                var resource = await _resourceRepo.GetByCodeAsync(
                    plan.BuildingId,
                    reservation.ClimateResourceId.ToString(),
                    ct);
                if (resource is not null)
                {
                    resource.Release(reservation.ReservedAmount);
                    await _resourceRepo.UpdateAsync(resource, ct);
                }
                reservation.Release();
            }
        }
    }
}
