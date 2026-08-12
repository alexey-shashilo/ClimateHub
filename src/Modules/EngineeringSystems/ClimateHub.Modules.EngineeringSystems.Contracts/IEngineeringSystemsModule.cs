using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Contracts;

public readonly record struct EngineeringCapabilityRequestId(Guid Value)
{
    public static EngineeringCapabilityRequestId New() => new(Guid.NewGuid());
    public static EngineeringCapabilityRequestId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public readonly record struct CommandPlanIdDto(Guid Value)
{
    public static CommandPlanIdDto From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public record EngineeringCapabilityRequestDto(
    EngineeringCapabilityRequestId RequestId,
    string NeedId,
    BuildingId BuildingId,
    RoomId RoomId,
    string CapabilityCode,
    string Severity,
    int Priority,
    double? DesiredEffect,
    double? CurrentValue,
    double? TargetValue,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ExpiresAt,
    string? CorrelationId,
    string? CausationId,
    string? IdempotencyKey);

public record CapabilityPlanResultDto(
    CommandPlanIdDto? CommandPlanId,
    bool Success,
    string? FailureCode,
    string? FailureReason);

public record CancelCommandPlanResultDto(bool Success, string? ErrorCode);

public record CommandPlanStatusDto(
    CommandPlanIdDto CommandPlanId,
    string Status,
    string? FailureCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public enum EngineeringRouteResultStatus
{
    Resolved,
    NotFound,
    Ambiguous,
    Unavailable,
    InsufficientResources,
    ControlDisabled,
    StrategyUnavailable
}

public interface IEngineeringSystemsModule
{
    Task<CapabilityPlanResultDto> PlanAsync(
        EngineeringCapabilityRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CommandPlanStatusDto?> GetCommandPlanStatusAsync(
        CommandPlanIdDto commandPlanId,
        CancellationToken cancellationToken = default);

    Task<CancelCommandPlanResultDto> CancelCommandPlanAsync(
        CommandPlanIdDto commandPlanId,
        CancellationToken cancellationToken = default);
}
