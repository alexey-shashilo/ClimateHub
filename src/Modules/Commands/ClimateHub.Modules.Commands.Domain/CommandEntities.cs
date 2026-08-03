using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Commands.Domain;

public class CommandAttempt
{
    public long Id { get; set; }
    public CommandId CommandId { get; set; }
    public int AttemptNumber { get; set; }
    public string? MqttMessageId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailureCode { get; set; }
    public DateTimeOffset? BrokerAcknowledgedAt { get; set; }
}

public class CommandOutbox
{
    public long Id { get; set; }
    public CommandId CommandId { get; set; }
    public DeviceId DeviceId { get; set; }
    public string Topic { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public DateTimeOffset? AvailableAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string? LastFailureCode { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class CommandInboxMessage
{
    public long Id { get; set; }
    public string Source { get; set; } = "";
    public string MessageId { get; set; } = "";
    public CommandId CommandId { get; set; }
    public string MessageType { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
    public string Status { get; set; } = "Received";
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? FailureCode { get; set; }
}