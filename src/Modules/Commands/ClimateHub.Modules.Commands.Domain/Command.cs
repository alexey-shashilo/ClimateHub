using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Commands.Domain;

public class Command : Entity<CommandId>, IAggregateRoot
{
    public BuildingId BuildingId { get; private init; }
    public RoomId? RoomId { get; private init; }
    public DeviceId DeviceId { get; private init; }
    public string CapabilityCode { get; private init; } = "";
    public string Operation { get; private init; } = "";
    public string ParametersJson { get; private init; } = "{}";
    public CommandPriority Priority { get; private init; }
    public CommandStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? ValidatedAt { get; private set; }
    public DateTimeOffset? QueuedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    public DateTimeOffset? ExecutionStartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? CreatedBy { get; private init; }
    public string? CorrelationId { get; private init; }
    public int CurrentAttemptNumber { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public Guid? ClientRequestId { get; private init; }
    public uint Version { get; private set; }

    private Command() { }

    private Command(CommandId id, BuildingId buildingId, DeviceId deviceId, string capabilityCode,
        string operation, string parametersJson, CommandPriority priority, DateTimeOffset? expiresAt,
        string? createdBy, string? correlationId, Guid? clientRequestId, RoomId? roomId)
    {
        Id = id;
        BuildingId = buildingId;
        RoomId = roomId;
        DeviceId = deviceId;
        CapabilityCode = capabilityCode;
        Operation = operation;
        ParametersJson = parametersJson;
        Priority = priority;
        ExpiresAt = expiresAt;
        CreatedBy = createdBy ?? "user:development";
        CorrelationId = correlationId;
        ClientRequestId = clientRequestId;
        Status = CommandStatus.Created;
        CreatedAt = DateTimeOffset.UtcNow;
        CurrentAttemptNumber = 0;
        Version = 1;
    }

    public static Command Create(CommandId id, BuildingId buildingId, DeviceId deviceId, string capabilityCode,
        string operation, string parametersJson, CommandPriority priority = CommandPriority.Normal,
        DateTimeOffset? expiresAt = null, string? createdBy = null, string? correlationId = null,
        Guid? clientRequestId = null, RoomId? roomId = null)
    {
        return new Command(id, buildingId, deviceId, capabilityCode, operation, parametersJson,
            priority, expiresAt, createdBy, correlationId, clientRequestId, roomId);
    }

    private void TransitionTo(CommandStatus to)
    {
        if (!CommandTransitions.IsValid(Status, to))
            throw new InvalidOperationException($"Cannot transition from {Status} to {to}");
        Status = to;
        Version++;
    }

    public void Validate() { TransitionTo(CommandStatus.Validated); ValidatedAt = DateTimeOffset.UtcNow; }
    public void Queue() { TransitionTo(CommandStatus.Queued); QueuedAt = DateTimeOffset.UtcNow; }
    public void Reject(string errorCode, string errorMessage)
    {
        TransitionTo(CommandStatus.Rejected);
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkPublished() { TransitionTo(CommandStatus.Published); PublishedAt = DateTimeOffset.UtcNow; CurrentAttemptNumber++; }

    public void Acknowledge()
    {
        if (Status == CommandStatus.CancellationRequested) { /* allow ack even during cancel */ }
        TransitionTo(CommandStatus.Acknowledged);
        AcknowledgedAt = DateTimeOffset.UtcNow;
    }

    public void StartExecution() { TransitionTo(CommandStatus.Executing); ExecutionStartedAt = DateTimeOffset.UtcNow; }
    public void CompleteSuccessfully() { TransitionTo(CommandStatus.Succeeded); CompletedAt = DateTimeOffset.UtcNow; }
    public void CompleteWithFailure(string errorCode, string errorMessage)
    {
        TransitionTo(CommandStatus.Failed);
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == CommandStatus.CancellationRequested) { TransitionTo(CommandStatus.Cancelled); CancelledAt = DateTimeOffset.UtcNow; }
        else { TransitionTo(CommandStatus.Cancelled); CancelledAt = DateTimeOffset.UtcNow; }
    }

    public void RequestCancellation() { TransitionTo(CommandStatus.CancellationRequested); }
    public void Expire() { TransitionTo(CommandStatus.Expired); CompletedAt = DateTimeOffset.UtcNow; }
    public void Timeout() { TransitionTo(CommandStatus.TimedOut); CompletedAt = DateTimeOffset.UtcNow; LastErrorCode = "EXECUTION_TIMEOUT"; }
    public void PublishDomainEvent(IDomainEvent evt) => RaiseDomainEvent(evt);
}

public record CommandCreatedEvent(CommandId CommandId, DeviceId DeviceId, string CapabilityCode, string Operation) : DomainEvent;
public record CommandStatusChangedEvent(CommandId CommandId, CommandStatus OldStatus, CommandStatus NewStatus) : DomainEvent;
