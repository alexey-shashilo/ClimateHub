namespace ClimateHub.Infrastructure.Audit;

public interface IAuditLogRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken ct = default);
    Task<List<AuditEvent>> GetByActorAsync(string actorId, int limit = 100, CancellationToken ct = default);
    Task<List<AuditEvent>> GetByBuildingAsync(string buildingId, int limit = 100, CancellationToken ct = default);
    Task<List<AuditEvent>> GetByActionAsync(string action, int limit = 100, CancellationToken ct = default);
}