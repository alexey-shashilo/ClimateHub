using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.Domain.ClimatePlans;

public readonly record struct ClimatePlanId(Guid Value)
{
    public static ClimatePlanId New() => new(Guid.NewGuid());
    public static ClimatePlanId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public enum ClimatePlanStatus
{
    Created,
    Planning,
    Planned,
    ReservingResources,
    Ready,
    Executing,
    WaitingForEffect,
    PartiallyCompleted,
    Completed,
    Blocked,
    Failed,
    Cancelled,
    Expired
}

public enum SubPlanStatus
{
    Pending,
    BlockedByDependency,
    Ready,
    Executing,
    EngineeringSucceeded,
    EngineeringFailed,
    WaitingForEffect,
    Completed,
    Skipped,
    Cancelled
}

public enum DependencyType
{
    FinishToStart,
    FinishToFinish,
    SafetyGate,
    ResourceGate,
    EffectGate
}

public enum ConflictType
{
    DirectOpposition,
    CrossSystemNegativeEffect,
    ResourceConflict,
    SafetyConflict,
    PolicyConflict
}

public class ClimatePlan : Entity<ClimatePlanId>, IAggregateRoot
{
    public ClimateGoalId GoalId { get; private init; }
    public BuildingId BuildingId { get; private init; }
    public RoomId RoomId { get; private init; }
    public StrategyProfile ActiveProfile { get; private set; }
    public ClimatePlanStatus Status { get; private set; }
    public int Priority { get; private set; }
    public string? PlanningReason { get; private set; }
    public string? EnvironmentSnapshotVersion { get; private set; }
    public string? PolicyVersion { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureReason { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public string? IdempotencyKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? PlannedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? WaitingForEffectAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private readonly List<EngineeringSubPlan> _subPlans = new();
    public IReadOnlyCollection<EngineeringSubPlan> SubPlans => _subPlans.AsReadOnly();

    private readonly List<ClimatePlanDependency> _dependencies = new();
    public IReadOnlyCollection<ClimatePlanDependency> Dependencies => _dependencies.AsReadOnly();

    private readonly List<ClimateConflict> _resolvedConflicts = new();
    public IReadOnlyCollection<ClimateConflict> ResolvedConflicts => _resolvedConflicts.AsReadOnly();

    private readonly List<ClimateResourceReservation> _resourceReservations = new();
    public IReadOnlyCollection<ClimateResourceReservation> ResourceReservations => _resourceReservations.AsReadOnly();

    public uint Version { get; private set; }

    private ClimatePlan()
    {
        PlanningReason = string.Empty;
    }

    private ClimatePlan(ClimatePlanId id, ClimateGoalId goalId, BuildingId buildingId, RoomId roomId,
        StrategyProfile profile, int priority = 100, string? planningReason = null,
        string? environmentSnapshotVersion = null, string? policyVersion = null,
        string? correlationId = null, string? causationId = null, string? idempotencyKey = null,
        DateTimeOffset? expiresAt = null)
    {
        Id = id; GoalId = goalId; BuildingId = buildingId; RoomId = roomId;
        ActiveProfile = profile; Priority = priority;
        Status = ClimatePlanStatus.Created;
        PlanningReason = planningReason;
        EnvironmentSnapshotVersion = environmentSnapshotVersion;
        PolicyVersion = policyVersion;
        CorrelationId = correlationId; CausationId = causationId;
        IdempotencyKey = idempotencyKey; ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow; Version = 1;
    }

    public static ClimatePlan Create(ClimateGoalId goalId, BuildingId buildingId, RoomId roomId,
        StrategyProfile profile, int priority = 100, string? planningReason = null,
        string? environmentSnapshotVersion = null, string? policyVersion = null,
        string? correlationId = null, string? causationId = null, string? idempotencyKey = null,
        DateTimeOffset? expiresAt = null) =>
        new(ClimatePlanId.New(), goalId, buildingId, roomId, profile, priority,
            planningReason, environmentSnapshotVersion, policyVersion,
            correlationId, causationId, idempotencyKey, expiresAt);

    public void AddSubPlan(EngineeringSubPlan subPlan) { _subPlans.Add(subPlan); Version++; }
    public void AddDependency(ClimatePlanDependency dependency) { _dependencies.Add(dependency); Version++; }
    public void AddConflict(ClimateConflict conflict) { _resolvedConflicts.Add(conflict); Version++; }
    public void AddReservation(ClimateResourceReservation reservation) { _resourceReservations.Add(reservation); Version++; }

    public void SetPlanning() { Transition(ClimatePlanStatus.Planning); }
    public void SetPlanned() { Transition(ClimatePlanStatus.Planned); PlannedAt = DateTimeOffset.UtcNow; }
    public void SetReservingResources() { Transition(ClimatePlanStatus.ReservingResources); }
    public void SetReady() { Transition(ClimatePlanStatus.Ready); }
    public void Start() { Transition(ClimatePlanStatus.Executing); StartedAt = DateTimeOffset.UtcNow; }

    public void WaitForEffect()
    {
        Transition(ClimatePlanStatus.WaitingForEffect);
        WaitingForEffectAt = DateTimeOffset.UtcNow;
    }

    public void PartiallyComplete()
    {
        Transition(ClimatePlanStatus.PartiallyCompleted);
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        Transition(ClimatePlanStatus.Completed);
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Block(string? code = null, string? reason = null)
    {
        Transition(ClimatePlanStatus.Blocked);
        FailureCode = code; FailureReason = reason;
    }

    public void Fail(string code, string reason)
    {
        Transition(ClimatePlanStatus.Failed);
        FailureCode = code; FailureReason = reason;
        FailedAt = DateTimeOffset.UtcNow; CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Transition(ClimatePlanStatus.Cancelled);
        CancelledAt = DateTimeOffset.UtcNow; CompletedAt = DateTimeOffset.UtcNow;
    }

    public void SetExpired()
    {
        Transition(ClimatePlanStatus.Expired);
        ExpiresAt = DateTimeOffset.UtcNow; CompletedAt = DateTimeOffset.UtcNow;
    }

    private bool IsTerminal(ClimatePlanStatus s) => s switch
    {
        ClimatePlanStatus.Completed => true,
        ClimatePlanStatus.PartiallyCompleted => true,
        ClimatePlanStatus.Failed => true,
        ClimatePlanStatus.Cancelled => true,
        ClimatePlanStatus.Expired => true,
        _ => false
    };

    private void Transition(ClimatePlanStatus to)
    {
        if (IsTerminal(Status))
            throw new InvalidOperationException($"ClimatePlan is already in terminal state {Status}");
        if (!IsValid(Status, to))
            throw new InvalidOperationException($"Invalid ClimatePlan transition: {Status} → {to}");
        Status = to;
        Version++;
    }

    private static bool IsValid(ClimatePlanStatus from, ClimatePlanStatus to) => (from, to) switch
    {
        (ClimatePlanStatus.Created, ClimatePlanStatus.Planning) => true,
        (ClimatePlanStatus.Created, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.Planning, ClimatePlanStatus.Planned) => true,
        (ClimatePlanStatus.Planning, ClimatePlanStatus.Blocked) => true,
        (ClimatePlanStatus.Planning, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.Planned, ClimatePlanStatus.ReservingResources) => true,
        (ClimatePlanStatus.Planned, ClimatePlanStatus.Blocked) => true,
        (ClimatePlanStatus.Planned, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.ReservingResources, ClimatePlanStatus.Ready) => true,
        (ClimatePlanStatus.ReservingResources, ClimatePlanStatus.Blocked) => true,
        (ClimatePlanStatus.ReservingResources, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.Ready, ClimatePlanStatus.Executing) => true,
        (ClimatePlanStatus.Ready, ClimatePlanStatus.Blocked) => true,
        (ClimatePlanStatus.Ready, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.Executing, ClimatePlanStatus.WaitingForEffect) => true,
        (ClimatePlanStatus.Executing, ClimatePlanStatus.PartiallyCompleted) => true,
        (ClimatePlanStatus.Executing, ClimatePlanStatus.Failed) => true,
        (ClimatePlanStatus.Executing, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.WaitingForEffect, ClimatePlanStatus.Completed) => true,
        (ClimatePlanStatus.WaitingForEffect, ClimatePlanStatus.PartiallyCompleted) => true,
        (ClimatePlanStatus.WaitingForEffect, ClimatePlanStatus.Failed) => true,
        (ClimatePlanStatus.WaitingForEffect, ClimatePlanStatus.Cancelled) => true,
        (ClimatePlanStatus.PartiallyCompleted, ClimatePlanStatus.Failed) => true,
        _ => false
    };
}

public class EngineeringSubPlan
{
    public Guid Id { get; private set; }
    public ClimatePlanId ClimatePlanId { get; set; }
    public string EngineeringCapabilityCode { get; private set; } = string.Empty;
    public double RequestedEffect { get; private set; }
    public string PriorityCategory { get; private set; } = "Medium";
    public int PriorityValue { get; private set; }
    public int ExecutionOrder { get; private set; }
    public SubPlanStatus Status { get; private set; }
    public string? EngineeringSystemId { get; private set; }
    public string? EngineeringCommandPlanId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public bool Required { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public uint Version { get; private set; }

    private EngineeringSubPlan() { }

    public EngineeringSubPlan(string engineeringCapabilityCode, double requestedEffect,
        string priorityCategory = "Medium", int priorityValue = 50, int executionOrder = 0,
        bool required = true, string? idempotencyKey = null)
    {
        Id = Guid.NewGuid();
        EngineeringCapabilityCode = engineeringCapabilityCode;
        RequestedEffect = requestedEffect;
        PriorityCategory = priorityCategory;
        PriorityValue = priorityValue;
        ExecutionOrder = executionOrder;
        Status = SubPlanStatus.Pending;
        Required = required;
        IdempotencyKey = idempotencyKey;
        Version = 1;
    }

    public void SetReady() { Status = SubPlanStatus.Ready; Version++; }
    public void BlockByDependency() { Status = SubPlanStatus.BlockedByDependency; Version++; }
    public void AssignSystem(string systemId) { EngineeringSystemId = systemId; Version++; }
    public void AssignCommandPlan(string commandPlanId) { EngineeringCommandPlanId = commandPlanId; Status = SubPlanStatus.Executing; StartedAt = DateTimeOffset.UtcNow; Version++; }
    public void MarkEngineeringSucceeded() { Status = SubPlanStatus.EngineeringSucceeded; Version++; }
    public void MarkEngineeringFailed(string? code = null) { Status = SubPlanStatus.EngineeringFailed; FailureCode = code; FailedAt = DateTimeOffset.UtcNow; Version++; }
    public void WaitForEffect() { Status = SubPlanStatus.WaitingForEffect; Version++; }
    public void Complete() { Status = SubPlanStatus.Completed; CompletedAt = DateTimeOffset.UtcNow; Version++; }
    public void Skip() { Status = SubPlanStatus.Skipped; CompletedAt = DateTimeOffset.UtcNow; Version++; }
    public void Cancel() { Status = SubPlanStatus.Cancelled; CompletedAt = DateTimeOffset.UtcNow; Version++; }

    public bool IsFinal() => Status is SubPlanStatus.Completed or SubPlanStatus.EngineeringFailed
        or SubPlanStatus.Skipped or SubPlanStatus.Cancelled;
}

public class ClimatePlanDependency
{
    public Guid Id { get; private set; }
    public ClimatePlanId ClimatePlanId { get; set; }
    public Guid PredecessorSubPlanId { get; private set; }
    public Guid SuccessorSubPlanId { get; private set; }
    public DependencyType Type { get; private set; }
    public bool Required { get; private set; }
    public string? Reason { get; private set; }

    private ClimatePlanDependency() { }

    public ClimatePlanDependency(Guid predecessorSubPlanId, Guid successorSubPlanId,
        DependencyType type = DependencyType.FinishToStart, bool required = true, string? reason = null)
    {
        Id = Guid.NewGuid();
        PredecessorSubPlanId = predecessorSubPlanId;
        SuccessorSubPlanId = successorSubPlanId;
        Type = type; Required = required; Reason = reason;
    }
}

public class ClimateConflict
{
    public Guid Id { get; private set; }
    public ClimatePlanId ClimatePlanId { get; set; }
    public ConflictType ConflictType { get; private set; }
    public string FirstCapabilityCode { get; private set; } = string.Empty;
    public string SecondCapabilityCode { get; private set; } = string.Empty;
    public string WinnerCapabilityCode { get; private set; } = string.Empty;
    public string LoserCapabilityCode { get; private set; } = string.Empty;
    public string Resolution { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public DateTimeOffset DetectedAt { get; private init; }

    private ClimateConflict() { }

    public ClimateConflict(Guid climatePlanId, ConflictType conflictType,
        string firstCapability, string secondCapability,
        string winnerCapability, string loserCapability,
        string resolution, string? reason = null)
    {
        Id = Guid.NewGuid(); ClimatePlanId = ClimatePlanId.From(climatePlanId);
        ConflictType = conflictType;
        FirstCapabilityCode = firstCapability;
        SecondCapabilityCode = secondCapability;
        WinnerCapabilityCode = winnerCapability;
        LoserCapabilityCode = loserCapability;
        Resolution = resolution; Reason = reason;
        DetectedAt = DateTimeOffset.UtcNow;
    }
}

public class ClimateResourceReservation
{
    public Guid Id { get; private set; }
    public Guid ClimateResourceId { get; private set; }
    public ClimatePlanId ClimatePlanId { get; set; }
    public Guid? EngineeringSubPlanId { get; private set; }
    public double RequestedAmount { get; private set; }
    public double ReservedAmount { get; private set; }
    public string Status { get; private set; } = "Requested";
    public DateTimeOffset RequestedAt { get; private init; }
    public DateTimeOffset? ReservedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public uint Version { get; private set; }

    private ClimateResourceReservation() { }

    public ClimateResourceReservation(Guid climateResourceId, Guid climatePlanId,
        Guid? engineeringSubPlanId, double requestedAmount, DateTimeOffset? expiresAt = null)
    {
        Id = Guid.NewGuid(); ClimateResourceId = climateResourceId;
        ClimatePlanId = ClimatePlanId.From(climatePlanId); EngineeringSubPlanId = engineeringSubPlanId;
        RequestedAmount = requestedAmount; Status = "Requested";
        RequestedAt = DateTimeOffset.UtcNow; ExpiresAt = expiresAt; Version = 1;
    }

    public void Reserve(double amount) { ReservedAmount = amount; Status = "Reserved"; ReservedAt = DateTimeOffset.UtcNow; Version++; }
    public void Allocate() { Status = "Allocated"; Version++; }
    public void Release() { Status = "Released"; ReleasedAt = DateTimeOffset.UtcNow; Version++; }
    public void Reject() { Status = "Rejected"; Version++; }
    public void Expire() { Status = "Expired"; ExpiresAt = DateTimeOffset.UtcNow; Version++; }
}
