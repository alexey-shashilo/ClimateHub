using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Domain;

// Local CommandId — avoids depending on Commands.Domain for a simple typed ID
public readonly record struct CommandId(Guid Value)
{
    public static CommandId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public readonly record struct NeedId(Guid Value)
{
    public static NeedId New() => new(Guid.NewGuid());
    public static NeedId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum NeedType
{
    TemperatureHeating, TemperatureCooling,
    HumidityIncrease, HumidityDecrease,
    Co2Reduction,
    IlluminanceIncrease, IlluminanceDecrease
}

public enum NeedStatus
{
    Detected, Planning, Planned, Executing,
    WaitingForEffect, Satisfied, Blocked, Cancelled, Expired
}

public enum NeedSeverity { Low, Medium, High, Critical }
public enum ControlMode { MonitorOnly, Manual, Automatic, Disabled }

public class Need : Entity<NeedId>, IAggregateRoot
{
    public BuildingId BuildingId { get; private init; }
    public RoomId RoomId { get; private init; }
    public NeedType Type { get; private init; }
    public NeedSeverity Severity { get; private set; }
    public NeedStatus Status { get; private set; }
    public ControlMode Mode { get; private set; }
    public double DesiredMin { get; private init; }
    public double DesiredMax { get; private init; }
    public double DesiredPreferred { get; private init; }
    public double? CurrentValue { get; private set; }
    public double Deviation { get; private set; }
    public string? SourceParameterCode { get; private set; }
    public DateTimeOffset? SourceMeasuredAt { get; private set; }
    public CommandId? ActiveCommandId { get; private set; }
    public CommandId? LastCommandId { get; private set; }
    public DeviceId? SelectedDeviceId { get; private set; }
    public string? SelectedCapabilityCode { get; private set; }
    public string? SelectedEngineeringSystemId { get; private set; }
    public string? SelectedEngineeringCapabilityCode { get; private set; }
    public string? ActiveCommandPlanId { get; private set; }
    public string? PlanningFailureCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? LastEvaluationAt { get; private set; }
    public DateTimeOffset? ViolationSince { get; private set; }
    public DateTimeOffset? StableSince { get; private set; }
    public DateTimeOffset? CooldownUntil { get; private set; }
    public DateTimeOffset? EffectEvaluationDueAt { get; private set; }
    public DateTimeOffset? LastCommandCreatedAt { get; private set; }
    public DateTimeOffset? LastMeaningfulImprovementAt { get; private set; }
    public int PlanningAttemptCount { get; private set; }
    public int CommandAttemptCount { get; private set; }
    public string? GeneratedByPolicyVersion { get; private init; }
    public uint Version { get; private set; }

    private Need() { }

    private Need(NeedId id, BuildingId buildingId, RoomId roomId, NeedType type,
        NeedSeverity severity, double min, double max, double preferred,
        double? currentValue, double deviation, string? parameterCode, ControlMode mode, string? policyVersion)
    {
        Id = id; BuildingId = buildingId; RoomId = roomId; Type = type;
        Severity = severity; Status = NeedStatus.Detected; Mode = mode;
        DesiredMin = min; DesiredMax = max; DesiredPreferred = preferred;
        CurrentValue = currentValue; Deviation = deviation;
        SourceParameterCode = parameterCode; CreatedAt = DateTimeOffset.UtcNow;
        GeneratedByPolicyVersion = policyVersion; Version = 1;
    }

    public static Need Create(BuildingId buildingId, RoomId roomId, NeedType type,
        NeedSeverity severity, double min, double max, double preferred,
        double? currentValue, double deviation, string? parameterCode,
        ControlMode mode = ControlMode.MonitorOnly, string? policyVersion = null) =>
        new(NeedId.New(), buildingId, roomId, type, severity, min, max, preferred,
            currentValue, deviation, parameterCode, mode, policyVersion);

    public void BeginPlanning() { Transition(NeedStatus.Planning); PlanningAttemptCount++; }

    public void MarkPlanned(DeviceId deviceId, string capabilityCode, CommandId? commandId = null)
    {
        Transition(NeedStatus.Planned);
        SelectedDeviceId = deviceId;
        SelectedCapabilityCode = capabilityCode;
        ActiveCommandId = commandId;
    }

    public void MarkEngineeringPlanned(string engineeringSystemId, string engineeringCapabilityCode, string commandPlanId)
    {
        Transition(NeedStatus.Planned);
        SelectedEngineeringSystemId = engineeringSystemId;
        SelectedEngineeringCapabilityCode = engineeringCapabilityCode;
        ActiveCommandPlanId = commandPlanId;
    }

    public void MarkExecuting(CommandId commandId)
    {
        Transition(NeedStatus.Executing);
        ActiveCommandId = commandId;
        LastCommandCreatedAt = DateTimeOffset.UtcNow;
        CommandAttemptCount++;
    }

    public void WaitForEffect(TimeSpan effectEvaluationDelay)
    {
        Transition(NeedStatus.WaitingForEffect);
        EffectEvaluationDueAt = DateTimeOffset.UtcNow.Add(effectEvaluationDelay);
    }

    public void Satisfy()
    {
        Transition(NeedStatus.Satisfied);
        ResolvedAt = DateTimeOffset.UtcNow;
        LastCommandId = ActiveCommandId;
        ActiveCommandId = null;
    }

    public void Block(string? failureCode = null)
    {
        Transition(NeedStatus.Blocked);
        PlanningFailureCode = failureCode;
    }

    public void ClearBlock()
    {
        PlanningFailureCode = null;
        if (Status == NeedStatus.Blocked)
            Status = NeedStatus.Detected;
        Version++;
    }

    public void Cancel()
    {
        Transition(NeedStatus.Cancelled);
        ResolvedAt = DateTimeOffset.UtcNow;
        LastCommandId = ActiveCommandId;
        ActiveCommandId = null;
    }

    public void ClearActiveCommand()
    {
        if (ActiveCommandId is not null)
            LastCommandId = ActiveCommandId;
        ActiveCommandId = null;
        if (Status is NeedStatus.Cancelled or NeedStatus.Satisfied) return;
        Status = NeedStatus.Detected;
        Version++;
    }

    public void Expire()
    {
        Transition(NeedStatus.Expired);
        ResolvedAt = DateTimeOffset.UtcNow;
        ActiveCommandId = null;
    }

    public void UpdateEvaluation(double? currentValue, double deviation, NeedSeverity severity,
        double? measuredAtUnixMs = null)
    {
        CurrentValue = currentValue;
        Deviation = deviation;
        Severity = severity;
        UpdatedAt = DateTimeOffset.UtcNow;
        LastEvaluationAt = DateTimeOffset.UtcNow;
        if (measuredAtUnixMs.HasValue)
            SourceMeasuredAt = DateTimeOffset.FromUnixTimeMilliseconds((long)(measuredAtUnixMs.Value * 1000));
        Version++;
    }

    public void SetViolationSince()
    {
        ViolationSince ??= DateTimeOffset.UtcNow;
    }

    public void ClearViolationSince()
    {
        ViolationSince = null;
    }

    public void SetStableSince()
    {
        StableSince ??= DateTimeOffset.UtcNow;
    }

    public void ClearStableSince()
    {
        StableSince = null;
    }

    public void SetCooldown(TimeSpan duration)
    {
        CooldownUntil = DateTimeOffset.UtcNow.Add(duration);
    }

    public void ClearCooldown()
    {
        CooldownUntil = null;
    }

    public void SetEffectEvaluationDue(TimeSpan delay)
    {
        EffectEvaluationDueAt = DateTimeOffset.UtcNow.Add(delay);
    }

    public void ClearEffectEvaluationDue()
    {
        EffectEvaluationDueAt = null;
    }

    public void RecordMeaningfulImprovement()
    {
        LastMeaningfulImprovementAt = DateTimeOffset.UtcNow;
    }

    private void Transition(NeedStatus to)
    {
        if (!IsValid(Status, to)) throw new InvalidOperationException($"Invalid Need transition: {Status} → {to}");
        Status = to;
        Version++;
    }

    private static bool IsValid(NeedStatus from, NeedStatus to) => (from, to) switch
    {
        (NeedStatus.Detected, NeedStatus.Planning) => true,
        (NeedStatus.Detected, NeedStatus.Cancelled) => true,
        (NeedStatus.Detected, NeedStatus.Satisfied) => true,
        (NeedStatus.Planning, NeedStatus.Planned) => true,
        (NeedStatus.Planning, NeedStatus.Blocked) => true,
        (NeedStatus.Planning, NeedStatus.Cancelled) => true,
        (NeedStatus.Planned, NeedStatus.Executing) => true,
        (NeedStatus.Planned, NeedStatus.Blocked) => true,
        (NeedStatus.Planned, NeedStatus.Cancelled) => true,
        (NeedStatus.Executing, NeedStatus.WaitingForEffect) => true,
        (NeedStatus.Executing, NeedStatus.Blocked) => true,
        (NeedStatus.Executing, NeedStatus.Cancelled) => true,
        (NeedStatus.WaitingForEffect, NeedStatus.Satisfied) => true,
        (NeedStatus.WaitingForEffect, NeedStatus.Executing) => true,
        (NeedStatus.WaitingForEffect, NeedStatus.Blocked) => true,
        (NeedStatus.WaitingForEffect, NeedStatus.Cancelled) => true,
        (NeedStatus.Blocked, NeedStatus.Detected) => true,
        (NeedStatus.Blocked, NeedStatus.Cancelled) => true,
        (NeedStatus.Blocked, NeedStatus.Expired) => true,
        (NeedStatus.Satisfied, NeedStatus.Expired) => true,
        _ => false
    };
}

public record NeedDto(NeedId Id, string Type, string Severity, string Status,
    double DesiredMin, double DesiredMax, double DesiredPreferred,
    double? CurrentValue, double Deviation, string? SourceParameterCode,
    RoomId RoomId, string? SelectedCapabilityCode, CommandId? ActiveCommandId,
    DeviceId? SelectedDeviceId, string? SelectedEngineeringSystemId,
    string? SelectedEngineeringCapabilityCode, string? ActiveCommandPlanId,
    string? PlanningFailureCode,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? ResolvedAt,
    DateTimeOffset? LastEvaluationAt, uint Version, ControlMode Mode,
    DateTimeOffset? ViolationSince, DateTimeOffset? StableSince,
    DateTimeOffset? CooldownUntil, DateTimeOffset? EffectEvaluationDueAt,
    DateTimeOffset? LastCommandCreatedAt, DateTimeOffset? LastMeaningfulImprovementAt,
    int PlanningAttemptCount, int CommandAttemptCount, CommandId? LastCommandId);
