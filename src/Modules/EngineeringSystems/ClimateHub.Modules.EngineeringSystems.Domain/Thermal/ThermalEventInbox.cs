namespace ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

public class ThermalEventInboxEntry
{
    public Guid Id { get; private set; }
    public string ConsumerName { get; private init; } = string.Empty;
    public string EventId { get; private init; } = string.Empty;
    public string EventType { get; private init; } = string.Empty;
    public string? AggregateId { get; private init; }
    public string Status { get; private set; } = "Pending";
    public DateTimeOffset ReceivedAt { get; private init; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? FailureCode { get; private set; }

    private ThermalEventInboxEntry() { }

    public ThermalEventInboxEntry(string consumerName, string eventId, string eventType, string? aggregateId = null)
    {
        Id = Guid.NewGuid(); ConsumerName = consumerName; EventId = eventId;
        EventType = eventType; AggregateId = aggregateId;
        ReceivedAt = DateTimeOffset.UtcNow; Status = "Pending";
    }

    public void StartProcessing() { Status = "Processing"; AttemptCount++; }
    public void Processed() { Status = "Processed"; ProcessedAt = DateTimeOffset.UtcNow; }
    public void Fail(string? code = null) { Status = "Failed"; FailureCode = code; }
}
