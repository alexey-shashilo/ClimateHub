namespace ClimateHub.Infrastructure.Audit;

public class AuditEvent
{
    public Guid AuditEventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string ActorType { get; private set; } = string.Empty;
    public string ActorId { get; private set; } = string.Empty;
    public string? SessionId { get; private set; }
    public string? BuildingId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? ResourceType { get; private set; }
    public string? ResourceId { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string? ReasonCode { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? TraceId { get; private set; }
    public string? Metadata { get; private set; }

    private AuditEvent() { }

    public AuditEvent(
        Guid auditEventId,
        string actorType,
        string actorId,
        string action,
        string outcome,
        string? sessionId = null,
        string? buildingId = null,
        string? resourceType = null,
        string? resourceId = null,
        string? reasonCode = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        string? traceId = null,
        string? metadata = null)
    {
        AuditEventId = auditEventId;
        OccurredAt = DateTime.UtcNow;
        ActorType = actorType;
        ActorId = actorId;
        SessionId = sessionId;
        BuildingId = buildingId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Outcome = outcome;
        ReasonCode = reasonCode;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        TraceId = traceId;
        Metadata = metadata;
    }
}