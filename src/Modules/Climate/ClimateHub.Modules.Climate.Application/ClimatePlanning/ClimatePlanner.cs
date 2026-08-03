using ClimateHub.Modules.Climate.Application.ConflictResolution;
using ClimateHub.Modules.Climate.Application.Execution;
using ClimateHub.Modules.Climate.Application.GoalPlanning;
using ClimateHub.Modules.Climate.Application.Prioritization;
using ClimateHub.Modules.Climate.Domain;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Application.ClimatePlanning;

public record ClimatePlanResult(ClimatePlan Plan, ConflictResolutionResult? ConflictResult);

public class ClimatePlanner
{
    private readonly GoalPlanner _goalPlanner;
    private readonly ConflictResolver _conflictResolver;
    private readonly PriorityEngine _priorityEngine;
    private readonly ClimateExecutionCoordinator _executionCoordinator;
    private readonly IEngineeringSystemsModule _engineeringSystems;

    public ClimatePlanner(
        GoalPlanner goalPlanner,
        ConflictResolver conflictResolver,
        PriorityEngine priorityEngine,
        ClimateExecutionCoordinator executionCoordinator,
        IEngineeringSystemsModule engineeringSystems)
    {
        _goalPlanner = goalPlanner;
        _conflictResolver = conflictResolver;
        _priorityEngine = priorityEngine;
        _executionCoordinator = executionCoordinator;
        _engineeringSystems = engineeringSystems;
    }

    public async Task<ClimatePlanResult> CreatePlanAsync(
        RoomId roomId, StrategyProfile profile,
        CancellationToken ct = default)
    {
        var goal = await _goalPlanner.CreateOrUpdateGoalAsync(roomId, profile, ct);

        var plan = ClimatePlan.Create(goal.Id, goal.BuildingId,
            goal.RoomId, profile);

        // Determine what capabilities are needed based on goal satisfaction
        var requestedCapabilities = new List<string>();

        if (goal.CurrentCo2.HasValue && goal.CurrentCo2.Value > goal.TargetCo2Max)
            requestedCapabilities.Add("eng.co2.reduce");

        if (goal.CurrentTemperature.HasValue)
        {
            if (goal.CurrentTemperature.Value < goal.TargetTemperatureMin)
                requestedCapabilities.Add("eng.temperature.increase");
            else if (goal.CurrentTemperature.Value > goal.TargetTemperatureMax)
                requestedCapabilities.Add("eng.temperature.decrease");
        }

        if (goal.CurrentHumidity.HasValue)
        {
            if (goal.CurrentHumidity.Value < goal.TargetHumidityMin)
                requestedCapabilities.Add("eng.humidity.increase");
            else if (goal.CurrentHumidity.Value > goal.TargetHumidityMax)
                requestedCapabilities.Add("eng.humidity.decrease");
        }

        // Resolve conflicts
        var resolution = _conflictResolver.Resolve(requestedCapabilities, profile);

        foreach (var conflict in resolution.Conflicts)
            plan.AddConflict(conflict);

        // Create sub-plans for each resolved capability
        var sortedCapabilities = _priorityEngine.SortByPriority(
            resolution.ResolvedCapabilities, profile);

        for (int i = 0; i < sortedCapabilities.Count; i++)
        {
            var cap = sortedCapabilities[i];
            var prio = _priorityEngine.GetCapabilityCategory(cap) switch
            {
                "Safety" or "EquipmentProtection" => 0,
                "IndoorAirQuality" => 1,
                "Temperature" => 2,
                "Humidity" => 3,
                _ => 4
            };
            plan.AddSubPlan(new EngineeringSubPlan(cap, 1.0,
                prio <= 1 ? "High" : prio <= 3 ? "Medium" : "Low", i));
        }

        // Blocked capabilities get recorded too
        foreach (var blocked in resolution.BlockedCapabilities)
        {
            plan.AddSubPlan(new EngineeringSubPlan(blocked, 0, "Blocked", 99));
        }

        plan.Start();
        return new ClimatePlanResult(plan, resolution);
    }

    public async Task<ClimatePlanResult> ExecutePlanAsync(
        ClimatePlan plan, RoomId roomId,
        CancellationToken ct = default)
    {
        var results = await _executionCoordinator.ExecuteSubPlansAsync(
            plan, roomId, ct);

        if (results.All(r => r.Success))
            plan.Complete();
        else if (results.Any(r => r.Success))
            plan.PartiallyComplete();
        else
            plan.Fail("CLIMATE_PLAN_EXECUTION_FAILED", "All engineering sub-plans failed");

        return new ClimatePlanResult(plan, null);
    }
}