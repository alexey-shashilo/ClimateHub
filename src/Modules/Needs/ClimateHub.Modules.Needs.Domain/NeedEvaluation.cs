using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Domain;

public class NeedEvaluation
{
    public long Id { get; private set; }
    public NeedId NeedId { get; private init; }
    public BuildingId BuildingId { get; private init; }
    public RoomId RoomId { get; private init; }
    public NeedEvaluationTrigger Trigger { get; private init; }
    public string? EnvironmentSnapshotJson { get; private init; }
    public string? PolicySnapshotJson { get; private init; }
    public string? PreviousStatus { get; private init; }
    public string? NewStatus { get; private init; }
    public string? CalculationResultJson { get; private init; }
    public string? CapabilityPlanJson { get; private init; }
    public string? DeviceResolutionResultJson { get; private init; }
    public CommandId? CommandId { get; private init; }
    public string? Outcome { get; private init; }
    public string? FailureCode { get; private init; }
    public DateTimeOffset EvaluatedAt { get; private init; }
    public string? CorrelationId { get; private init; }
    public string? CausationId { get; private init; }

    private NeedEvaluation() { }

    public static NeedEvaluation Create(
        NeedId needId, BuildingId buildingId, RoomId roomId,
        NeedEvaluationTrigger trigger,
        string? previousStatus, string? newStatus,
        string? calculationResultJson = null,
        string? capabilityPlanJson = null,
        string? deviceResolutionResultJson = null,
        CommandId? commandId = null,
        string? outcome = null,
        string? failureCode = null,
        string? correlationId = null,
        string? causationId = null,
        string? environmentSnapshotJson = null,
        string? policySnapshotJson = null)
    {
        return new NeedEvaluation
        {
            NeedId = needId,
            BuildingId = buildingId,
            RoomId = roomId,
            Trigger = trigger,
            EnvironmentSnapshotJson = environmentSnapshotJson,
            PolicySnapshotJson = policySnapshotJson,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CalculationResultJson = calculationResultJson,
            CapabilityPlanJson = capabilityPlanJson,
            DeviceResolutionResultJson = deviceResolutionResultJson,
            CommandId = commandId,
            Outcome = outcome,
            FailureCode = failureCode,
            EvaluatedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }
}
