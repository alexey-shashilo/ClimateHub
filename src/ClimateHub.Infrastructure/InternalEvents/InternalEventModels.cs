namespace ClimateHub.Infrastructure.InternalEvents;

public enum InternalEventStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}

public enum InternalEventType
{
    EnvironmentStateChanged,
    RoomPolicyChanged,
    DeviceConnectivityChanged,
    DeviceAssignmentChanged,
    DeviceCapabilityChanged,
    CommandSucceeded,
    CommandFailed,
    CommandTimedOut,
    CommandCancelled,
    CommandRejected,
    NeedDetected,
    NeedUpdated,
    NeedPlanning,
    NeedPlanned,
    NeedExecuting,
    NeedWaitingForEffect,
    NeedBlocked,
    NeedUnblocked,
    NeedSatisfied,
    NeedCancelled,
    NeedExpired,
    NeedControlModeChanged
}

public class InternalEventOutbox
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string? AggregateId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? RoomId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Payload { get; set; }
    public string? Headers { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastFailureCode { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class InternalEventInbox
{
    public long Id { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? FailureCode { get; set; }
    public string? PayloadHash { get; set; }
}

public class SseEventLog
{
    public long Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? BuildingId { get; set; }
    public Guid? RoomId { get; set; }
    public string? Payload { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public record InternalEventEnvelope(
    Guid EventId,
    string EventType,
    string AggregateType,
    string? AggregateId,
    Guid? BuildingId,
    Guid? RoomId,
    DateTimeOffset OccurredAt,
    string? Payload,
    string? Headers,
    string? CorrelationId,
    string? CausationId);
