using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Infrastructure.Audit;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditLogDbContext _db;

    public AuditLogRepository(AuditLogDbContext db) => _db = db;

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        _db.AuditEvents.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<List<AuditEvent>> GetByActorAsync(string actorId, int limit = 100, CancellationToken ct = default) =>
        _db.AuditEvents
            .Where(e => e.ActorId == actorId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<List<AuditEvent>> GetByBuildingAsync(string buildingId, int limit = 100, CancellationToken ct = default) =>
        _db.AuditEvents
            .Where(e => e.BuildingId == buildingId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<List<AuditEvent>> GetByActionAsync(string action, int limit = 100, CancellationToken ct = default) =>
        _db.AuditEvents
            .Where(e => e.Action == action)
            .OrderByDescending(e => e.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
}