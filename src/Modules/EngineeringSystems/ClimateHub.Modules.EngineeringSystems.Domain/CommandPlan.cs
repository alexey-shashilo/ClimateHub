using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Domain;

public enum StrategyType { RuleBased, Linear, Step, Pid, OnOff, Adaptive, Scheduled, RuleBasedAdvanced }

public enum CommandPlanStatus
{
    Requested,
    Reserved,
    Allocated,
    Planning,
    Planned,
    Ready,
    WaitingForCommands,
    Executing,
    Succeeded,
    PartiallySucceeded,
    Completed,
    Failed,
    Cancelled,
    Expired,
    Blocked
}

public enum CommandPlanStepStatus { Pending, Ready, Executing, Succeeded, Failed, Cancelled, Skipped }

public enum ExecutionMode { Sequential = 0, Parallel = 1 }

public class Strategy
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public StrategyType Type { get; private set; }
    public string? ConfigurationJson { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? EngineeringSystemId { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Strategy()
    {
        Name = string.Empty;
    }

    private Strategy(Guid id, string name, StrategyType type, string? configurationJson, Guid? engineeringSystemId = null)
    {
        Id = id; Name = name; Type = type;
        ConfigurationJson = configurationJson; IsActive = true;
        EngineeringSystemId = engineeringSystemId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Strategy Create(string name, StrategyType type, string? configurationJson = null, Guid? engineeringSystemId = null) =>
        new(Guid.NewGuid(), name, type, configurationJson, engineeringSystemId);

    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
    public void UpdateConfiguration(string json) { ConfigurationJson = json; UpdatedAt = DateTimeOffset.UtcNow; }
    public void AssignToSystem(Guid engineeringSystemId) { EngineeringSystemId = engineeringSystemId; UpdatedAt = DateTimeOffset.UtcNow; }
}

public class CommandPlan : Entity<Guid>, IAggregateRoot
{
    public EngineeringSystemId EngineeringSystemId { get; private init; }
    public string NeedType { get; private init; }
    public string CapabilityCode { get; private init; }
    public CommandPlanStatus Status { get; private set; }
    public double RequestedValue { get; private set; }
    public string? ValueUnit { get; private set; }
    public string? StrategyName { get; private set; }
    public Guid? StrategyStrategyId { get; private set; }
    public string? RequestedEffect { get; private set; }
    public Guid? ResourceAllocationId { get; private set; }
    public string? NeedId { get; private set; }
    public BuildingId? BuildingId { get; private set; }
    public RoomId? RoomId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? PlannedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? FailureCode { get; private set; }

    private readonly List<CommandPlanStep> _steps = new();
    public IReadOnlyCollection<CommandPlanStep> Steps => _steps.AsReadOnly();

    private readonly List<ResourceAllocation> _resourceAllocations = new();
    public IReadOnlyCollection<ResourceAllocation> ResourceAllocations => _resourceAllocations.AsReadOnly();

    public uint Version { get; private set; }

    private CommandPlan()
    {
        NeedType = string.Empty;
        CapabilityCode = string.Empty;
    }

    private CommandPlan(EngineeringSystemId systemId, string needType, string capabilityCode,
        double requestedValue, string? valueUnit, string? strategyName,
        List<CommandPlanStep> steps,
        string? needId = null, BuildingId? buildingId = null, RoomId? roomId = null,
        Guid? strategyStrategyId = null, string? requestedEffect = null,
        string? correlationId = null, string? causationId = null, string? idempotencyKey = null,
        DateTimeOffset? expiresAt = null)
    {
        Id = Guid.NewGuid(); EngineeringSystemId = systemId;
        NeedType = needType; CapabilityCode = capabilityCode;
        Status = CommandPlanStatus.Requested;
        RequestedValue = requestedValue; ValueUnit = valueUnit;
        StrategyName = strategyName; CreatedAt = DateTimeOffset.UtcNow;
        _steps = steps; Version = 1;
        NeedId = needId; BuildingId = buildingId; RoomId = roomId;
        StrategyStrategyId = strategyStrategyId; RequestedEffect = requestedEffect;
        CorrelationId = correlationId; CausationId = causationId; IdempotencyKey = idempotencyKey;
        ExpiresAt = expiresAt;
    }

    public static CommandPlan Create(EngineeringSystemId systemId, string needType,
        string capabilityCode, double requestedValue, string? valueUnit,
        string? strategyName, List<CommandPlanStep> steps,
        string? needId = null, BuildingId? buildingId = null, RoomId? roomId = null,
        Guid? strategyStrategyId = null, string? requestedEffect = null,
        string? correlationId = null, string? causationId = null, string? idempotencyKey = null,
        DateTimeOffset? expiresAt = null) =>
        new(systemId, needType, capabilityCode, requestedValue, valueUnit, strategyName, steps,
            needId, buildingId, roomId, strategyStrategyId, requestedEffect,
            correlationId, causationId, idempotencyKey, expiresAt);

    public void Reserve() => Transition(CommandPlanStatus.Reserved);
    public void Allocate() => Transition(CommandPlanStatus.Allocated);
    public void SetPlanning() { Transition(CommandPlanStatus.Planning); }
    public void SetReady() { Transition(CommandPlanStatus.Ready); }
    public void SetWaitingForCommands() { Transition(CommandPlanStatus.WaitingForCommands); }
    public void StartExecuting() { Transition(CommandPlanStatus.Executing); StartedAt ??= DateTimeOffset.UtcNow; }
    public void SetSucceeded() { Transition(CommandPlanStatus.Succeeded); CompletedAt = DateTimeOffset.UtcNow; }
    public void SetPartiallySucceeded() { Transition(CommandPlanStatus.PartiallySucceeded); CompletedAt = DateTimeOffset.UtcNow; }

    public void Complete()
    {
        Transition(CommandPlanStatus.Completed);
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string failureCode)
    {
        Transition(CommandPlanStatus.Failed);
        FailureCode = failureCode; FailedAt = DateTimeOffset.UtcNow; CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Transition(CommandPlanStatus.Cancelled);
        CancelledAt = DateTimeOffset.UtcNow; CompletedAt = DateTimeOffset.UtcNow;
    }

    public void SetExpired()
    {
        Transition(CommandPlanStatus.Expired);
        ExpiresAt = DateTimeOffset.UtcNow; CompletedAt = DateTimeOffset.UtcNow;
    }

    public void SetBlocked(string? failureCode)
    {
        Transition(CommandPlanStatus.Blocked);
        FailureCode = failureCode; CompletedAt = DateTimeOffset.UtcNow;
    }

    public void AddResourceAllocation(string resourceCode, double amount)
    {
        _resourceAllocations.Add(new ResourceAllocation(resourceCode, amount));
        Version++;
    }

    public void SetResourceAllocationId(Guid resourceAllocationId)
    {
        ResourceAllocationId = resourceAllocationId;
        Version++;
    }

    private bool IsTerminal(CommandPlanStatus s) => s switch
    {
        CommandPlanStatus.Completed => true,
        CommandPlanStatus.Succeeded => true,
        CommandPlanStatus.PartiallySucceeded => true,
        CommandPlanStatus.Failed => true,
        CommandPlanStatus.Cancelled => true,
        CommandPlanStatus.Expired => true,
        CommandPlanStatus.Blocked => true,
        _ => false
    };

    private void Transition(CommandPlanStatus to)
    {
        if (IsTerminal(Status))
            throw new InvalidOperationException($"CommandPlan is already in terminal state {Status}");
        if (!IsValid(Status, to))
            throw new InvalidOperationException($"Invalid CommandPlan transition: {Status} → {to}");
        Status = to;
        Version++;
    }

    private static bool IsValid(CommandPlanStatus from, CommandPlanStatus to) => (from, to) switch
    {
        (CommandPlanStatus.Requested, CommandPlanStatus.Reserved) => true,
        (CommandPlanStatus.Requested, CommandPlanStatus.Planning) => true,
        (CommandPlanStatus.Requested, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.Reserved, CommandPlanStatus.Allocated) => true,
        (CommandPlanStatus.Reserved, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.Allocated, CommandPlanStatus.Executing) => true,
        (CommandPlanStatus.Allocated, CommandPlanStatus.Failed) => true,
        (CommandPlanStatus.Allocated, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.Planning, CommandPlanStatus.Planned) => true,
        (CommandPlanStatus.Planning, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.Planned, CommandPlanStatus.Ready) => true,
        (CommandPlanStatus.Planned, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.Ready, CommandPlanStatus.Executing) => true,
        (CommandPlanStatus.Ready, CommandPlanStatus.Blocked) => true,
        (CommandPlanStatus.Ready, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.Executing, CommandPlanStatus.WaitingForCommands) => true,
        (CommandPlanStatus.Executing, CommandPlanStatus.Succeeded) => true,
        (CommandPlanStatus.Executing, CommandPlanStatus.PartiallySucceeded) => true,
        (CommandPlanStatus.Executing, CommandPlanStatus.Failed) => true,
        (CommandPlanStatus.Executing, CommandPlanStatus.Cancelled) => true,
        (CommandPlanStatus.WaitingForCommands, CommandPlanStatus.Succeeded) => true,
        (CommandPlanStatus.WaitingForCommands, CommandPlanStatus.Failed) => true,
        (CommandPlanStatus.WaitingForCommands, CommandPlanStatus.Cancelled) => true,
        _ => false
    };
}

public class CommandPlanStep
{
    public Guid Id { get; private set; }
    public Guid CommandPlanId { get; set; }
    public string CapabilityCode { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public double RequestedValue { get; private set; }
    public string? ValueUnit { get; private set; }
    public Guid? DeviceId { get; set; }
    public string? DeviceRole { get; private set; }
    public int Sequence { get; private set; }
    public ExecutionMode ExecutionMode { get; private set; }
    public bool Required { get; private set; }
    public CommandPlanStepStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }
    public uint Version { get; private set; }

    private CommandPlanStep()
    {
        CapabilityCode = string.Empty;
        Operation = string.Empty;
    }

    public CommandPlanStep(string capabilityCode, string operation, double requestedValue,
        string? valueUnit = null, Guid? deviceId = null, string? deviceRole = null,
        int sequence = 0, ExecutionMode executionMode = ExecutionMode.Sequential,
        bool required = true)
    {
        Id = Guid.NewGuid(); CapabilityCode = capabilityCode;
        Operation = operation; RequestedValue = requestedValue;
        ValueUnit = valueUnit; DeviceId = deviceId; DeviceRole = deviceRole;
        Sequence = sequence; ExecutionMode = executionMode; Required = required;
        Status = CommandPlanStepStatus.Pending;
        Version = 1;
    }

    public void SetReady() { Status = CommandPlanStepStatus.Ready; Version++; }
    public void Start() { Status = CommandPlanStepStatus.Executing; StartedAt = DateTimeOffset.UtcNow; Version++; }
    public void Succeed() { Status = CommandPlanStepStatus.Succeeded; CompletedAt = DateTimeOffset.UtcNow; Version++; }
    public void Fail(string? failureCode) { Status = CommandPlanStepStatus.Failed; FailureCode = failureCode; CompletedAt = DateTimeOffset.UtcNow; Version++; }
    public void Cancel() { Status = CommandPlanStepStatus.Cancelled; CompletedAt = DateTimeOffset.UtcNow; Version++; }
    public void Skip() { Status = CommandPlanStepStatus.Skipped; CompletedAt = DateTimeOffset.UtcNow; Version++; }
}

public class ResourceAllocation
{
    public Guid Id { get; private set; }
    public Guid CommandPlanId { get; set; }
    public string ResourceCode { get; private set; } = string.Empty;
    public double Amount { get; private set; }
    public DateTimeOffset AllocatedAt { get; private init; }

    private ResourceAllocation()
    {
        ResourceCode = string.Empty;
    }

    public ResourceAllocation(string resourceCode, double amount)
    {
        Id = Guid.NewGuid(); ResourceCode = resourceCode;
        Amount = amount; AllocatedAt = DateTimeOffset.UtcNow;
    }
}
