using ClimateHub.Modules.Climate.Application.ClimatePlanning;
using ClimateHub.Modules.Climate.Application.ConflictResolution;
using ClimateHub.Modules.Climate.Application.GoalPlanning;
using ClimateHub.Modules.Climate.Application.Prioritization;
using ClimateHub.Modules.Climate.Contracts;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Application;

public class ClimateModuleService : IClimateModule
{
    private readonly ClimatePlanner _climatePlanner;
    private readonly GoalPlanner _goalPlanner;
    private readonly PriorityEngine _priorityEngine;
    private readonly ConflictResolver _conflictResolver;

    public ClimateModuleService(
        ClimatePlanner climatePlanner,
        GoalPlanner goalPlanner,
        PriorityEngine priorityEngine,
        ConflictResolver conflictResolver)
    {
        _climatePlanner = climatePlanner;
        _goalPlanner = goalPlanner;
        _priorityEngine = priorityEngine;
        _conflictResolver = conflictResolver;
    }

    public async Task<ClimatePlanResultDtoFull> PlanRoomAsync(RoomId roomId,
        string profile = "Comfort", CancellationToken ct = default)
    {
        var parsedProfile = Enum.TryParse<StrategyProfile>(profile, true, out var p)
            ? p : StrategyProfile.Comfort;
        var result = await _climatePlanner.CreatePlanAsync(roomId, parsedProfile, ct);
        return MapToDto(result);
    }

    public async Task<ClimateGoalDto> GetGoalForRoomAsync(RoomId roomId,
        CancellationToken ct = default)
    {
        var goal = await _goalPlanner.CreateOrUpdateGoalAsync(roomId, StrategyProfile.Comfort, ct);
        return MapGoalToDto(goal);
    }

    private static ClimateGoalDto MapGoalToDto(ClimateGoal g) => new(
        g.Id.ToString(), g.RoomId.ToString(), g.Status.ToString(),
        g.ActiveProfile.ToString(), Math.Round(g.SatisfactionPct, 1),
        g.TargetTemperature, g.TargetTemperatureMin, g.TargetTemperatureMax,
        g.TargetHumidity, g.TargetHumidityMin, g.TargetHumidityMax,
        g.TargetCo2, g.TargetCo2Max,
        g.CurrentTemperature, g.CurrentHumidity, g.CurrentCo2,
        g.CreatedAt, g.LastEvaluatedAt,
        g.ActiveClimatePlanId?.ToString());

    private static ClimatePlanResultDtoFull MapToDto(ClimatePlanning.ClimatePlanResult result)
    {
        var plan = result.Plan;
        return new ClimatePlanResultDtoFull(
            new ClimatePlanDto(
                plan.Id.ToString(), plan.GoalId.ToString(), plan.RoomId.ToString(),
                plan.Status.ToString(), plan.ActiveProfile.ToString(),
                plan.SubPlans.Select(sp => new EngineeringSubPlanDto(
                    sp.Id.ToString(), sp.EngineeringCapabilityCode,
                    sp.Status.ToString(), sp.PriorityCategory,
                    sp.ExecutionOrder, sp.EngineeringSystemId,
                    sp.EngineeringCommandPlanId)).ToList(),
                plan.Dependencies.Select(d => new ClimatePlanDependencyDto(
                    d.PredecessorSubPlanId, d.SuccessorSubPlanId,
                    d.Type.ToString(), d.Required, d.Reason)).ToList(),
                plan.ResolvedConflicts.Select(c => new ClimateConflictDto(
                    c.ConflictType.ToString(), c.FirstCapabilityCode,
                    c.SecondCapabilityCode, c.WinnerCapabilityCode,
                    c.LoserCapabilityCode, c.Resolution, c.Reason)).ToList(),
                plan.FailureCode, plan.FailureReason,
                plan.CreatedAt, plan.StartedAt, plan.CompletedAt),
            result.ConflictResult is null ? null : new ConflictResolutionResultDto(
                result.ConflictResult.ResolvedCapabilities,
                result.ConflictResult.BlockedCapabilities,
                result.ConflictResult.Conflicts.Select(c => new ClimateConflictDto(
                    c.ConflictType.ToString(), c.FirstCapabilityCode,
                    c.SecondCapabilityCode, c.WinnerCapabilityCode,
                    c.LoserCapabilityCode, c.Resolution, c.Reason)).ToList()));
    }

    public List<string> GetStrategyProfiles() =>
        Enum.GetNames<StrategyProfile>().ToList();

    public bool IsHigherPriority(string capabilityA, string capabilityB, string profile)
    {
        var parsedProfile = Enum.TryParse<StrategyProfile>(profile, true, out var p)
            ? p : StrategyProfile.Comfort;
        return _priorityEngine.IsHigherPriorityThan(capabilityA, capabilityB, parsedProfile);
    }

    public bool ConflictsExist(List<string> capabilities)
    {
        var result = _conflictResolver.Resolve(capabilities, StrategyProfile.Balanced);
        return result.Conflicts.Count > 0;
    }
}
