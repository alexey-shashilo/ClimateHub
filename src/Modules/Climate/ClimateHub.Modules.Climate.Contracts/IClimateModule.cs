using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Contracts;

public record ClimateGoalDto(
    string GoalId,
    string RoomId,
    string Status,
    string Profile,
    double SatisfactionPct,
    double TargetTemperature,
    double TargetTemperatureMin,
    double TargetTemperatureMax,
    double TargetHumidity,
    double TargetHumidityMin,
    double TargetHumidityMax,
    double TargetCo2,
    double TargetCo2Max,
    double? CurrentTemperature,
    double? CurrentHumidity,
    double? CurrentCo2,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastEvaluatedAt,
    string? ActiveClimatePlanId);

public record EngineeringSubPlanDto(
    string Id,
    string CapabilityCode,
    string Status,
    string Priority,
    int ExecutionOrder,
    string? EngineeringSystemId,
    string? EngineeringCommandPlanId);

public record ClimatePlanDependencyDto(
    Guid PredecessorId,
    Guid SuccessorId,
    string Type,
    bool Required,
    string? Reason);

public record ClimateConflictDto(
    string ConflictType,
    string FirstCapability,
    string SecondCapability,
    string WinnerCapability,
    string LoserCapability,
    string Resolution,
    string? Reason);

public record ClimatePlanDto(
    string PlanId,
    string GoalId,
    string RoomId,
    string Status,
    string Profile,
    List<EngineeringSubPlanDto> SubPlans,
    List<ClimatePlanDependencyDto> Dependencies,
    List<ClimateConflictDto> Conflicts,
    string? FailureCode,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public record ClimatePlanResultDto(
    string? PlanId,
    bool Success,
    string? FailureCode,
    string? FailureReason);

public record ConflictResolutionResultDto(
    List<string> ResolvedCapabilities,
    List<string> BlockedCapabilities,
    List<ClimateConflictDto> Conflicts);

public record ClimatePlanResultDtoFull(
    ClimatePlanDto? Plan,
    ConflictResolutionResultDto? ConflictResult);

public interface IClimateModule
{
    Task<ClimatePlanResultDtoFull> PlanRoomAsync(RoomId roomId,
        string profile = "Comfort", CancellationToken ct = default);

    Task<ClimateGoalDto> GetGoalForRoomAsync(RoomId roomId,
        CancellationToken ct = default);

    List<string> GetStrategyProfiles();

    bool IsHigherPriority(string capabilityA, string capabilityB, string profile);

    bool ConflictsExist(List<string> capabilities);
}
