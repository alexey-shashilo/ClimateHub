using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Infrastructure.InternalEvents;

public class SseEventLogRepository
{
    private readonly InternalEventsDbContext _ctx;

    public SseEventLogRepository(InternalEventsDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<long> AppendAsync(Guid eventId, string eventType, Guid? buildingId, Guid? roomId,
        string? payload, DateTimeOffset occurredAt, CancellationToken ct = default)
    {
        var entity = new SseEventLog
        {
            EventId = eventId,
            EventType = eventType,
            BuildingId = buildingId,
            RoomId = roomId,
            Payload = payload,
            OccurredAt = occurredAt
        };
        await _ctx.SseEventLog.AddAsync(entity, ct);
        await _ctx.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<List<SseEventLog>> GetEventsSinceIdAsync(long lastEventId, int maxEvents = 100, CancellationToken ct = default)
    {
        return await _ctx.SseEventLog
            .Where(x => x.Id > lastEventId)
            .OrderBy(x => x.Id)
            .Take(maxEvents)
            .ToListAsync(ct);
    }

    public async Task<List<SseEventLog>> GetEventsByRoomSinceIdAsync(Guid roomId, long lastEventId, int maxEvents = 100, CancellationToken ct = default)
    {
        return await _ctx.SseEventLog
            .Where(x => x.RoomId == roomId && x.Id > lastEventId)
            .OrderBy(x => x.Id)
            .Take(maxEvents)
            .ToListAsync(ct);
    }

    public async Task<long> GetMaxEventIdAsync(CancellationToken ct = default)
    {
        return await _ctx.SseEventLog.MaxAsync(x => (long?)x.Id, ct) ?? 0;
    }

    public async Task CleanupAsync(TimeSpan retention, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(retention);
        await _ctx.SseEventLog
            .Where(x => x.OccurredAt <= cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
