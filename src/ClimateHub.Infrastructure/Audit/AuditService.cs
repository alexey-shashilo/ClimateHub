namespace ClimateHub.Infrastructure.Audit;

public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(
        string actorType,
        string actorId,
        string action,
        string outcome,
        string? buildingId = null,
        string? resourceType = null,
        string? resourceId = null,
        string? reasonCode = null,
        string? sessionId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        string? traceId = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        var auditEvent = new AuditEvent(
            Guid.NewGuid(),
            actorType,
            actorId,
            action,
            outcome,
            sessionId,
            buildingId,
            resourceType,
            resourceId,
            reasonCode,
            ipAddress,
            userAgent,
            correlationId,
            traceId,
            metadata);

        await _auditLogRepository.AppendAsync(auditEvent, ct);
    }
}
