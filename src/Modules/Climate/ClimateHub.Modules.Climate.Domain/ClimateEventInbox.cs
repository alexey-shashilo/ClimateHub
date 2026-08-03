namespace ClimateHub.Modules.Climate.Domain;

public class ClimateEventInboxEntry
{
    public Guid Id { get; private set; }
    public string ConsumerName { get; private init; } = string.Empty;
    public string EventId { get; private init; } = string.Empty;
    public string EventType { get; private init; } = string.Empty;
    public string? AggregateId { get; private init; }
    public string? BuildingId { get; private init; }
    public string? RoomId { get; private init; }
    public DateTimeOffset ReceivedAt { get; private init; }
    public string Status { get; private set; } = "Pending";
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? FailureCode { get; private set; }
    public string? PayloadHash { get; private init; }
    public DateTimeOffset? AvailableAt { get; private set; }

    private ClimateEventInboxEntry() { }

    public ClimateEventInboxEntry(string consumerName, string eventId, string eventType,
        string? aggregateId = null, string? buildingId = null, string? roomId = null,
        string? payloadHash = null)
    {
        Id = Guid.NewGuid(); ConsumerName = consumerName; EventId = eventId;
        EventType = eventType; AggregateId = aggregateId;
        BuildingId = buildingId; RoomId = roomId;
        PayloadHash = payloadHash; ReceivedAt = DateTimeOffset.UtcNow;
        Status = "Pending";
    }

    public void StartProcessing() { Status = "Processing"; ProcessingStartedAt = DateTimeOffset.UtcNow; AttemptCount++; }
    public void Processed() { Status = "Processed"; ProcessedAt = DateTimeOffset.UtcNow; }
    public void Fail(string? code = null) { Status = "Failed"; FailureCode = code; }
    public void RetryLater(TimeSpan delay) { AvailableAt = DateTimeOffset.UtcNow.Add(delay); Status = "Pending"; }
}

public class ClimateEvaluationEntry
{
    public Guid Id { get; private set; }
    public Guid? ClimateGoalId { get; private set; }
    public Guid? ClimatePlanId { get; private set; }
    public string BuildingId { get; private init; } = string.Empty;
    public string RoomId { get; private init; } = string.Empty;
    public string Trigger { get; private init; } = string.Empty;
    public string? EnvironmentSnapshot { get; private set; }
    public string? PolicySnapshot { get; private set; }
    public string? NeedSnapshot { get; private set; }
    public string? GoalBefore { get; private set; }
    public string? GoalAfter { get; private set; }
    public string? PlanningResult { get; private set; }
    public string? ConflictResolution { get; private set; }
    public string? DependencyGraph { get; private set; }
    public string? ResourceResult { get; private set; }
    public string? Outcome { get; private set; }
    public string? FailureCode { get; private set; }
    public DateTimeOffset EvaluatedAt { get; private init; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }

    private ClimateEvaluationEntry() { }

    public ClimateEvaluationEntry(Guid buildingId, Guid roomId, string trigger,
        Guid? climateGoalId = null, Guid? climatePlanId = null,
        string? correlationId = null, string? causationId = null)
    {
        Id = Guid.NewGuid(); BuildingId = buildingId.ToString();
        RoomId = roomId.ToString(); Trigger = trigger;
        ClimateGoalId = climateGoalId; ClimatePlanId = climatePlanId;
        CorrelationId = correlationId; CausationId = causationId;
        EvaluatedAt = DateTimeOffset.UtcNow;
    }
}