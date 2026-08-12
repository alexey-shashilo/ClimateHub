using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Infrastructure.InternalEvents;

public class InternalEventOutboxRepository
{
    private readonly InternalEventsDbContext _ctx;

    public InternalEventOutboxRepository(InternalEventsDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<Guid> CreateAsync(
        string eventType,
        string aggregateType,
        string? aggregateId,
        Guid? buildingId,
        Guid? roomId,
        object? payload,
        Dictionary<string, string>? headers,
        string? correlationId,
        string? causationId,
        DateTimeOffset? occurredAt = null,
        CancellationToken ct = default)
    {
        var eventId = Guid.NewGuid();
        var now = occurredAt ?? DateTimeOffset.UtcNow;

        var entity = new InternalEventOutbox
        {
            EventId = eventId,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            BuildingId = buildingId,
            RoomId = roomId,
            OccurredAt = now,
            Payload = payload is not null ? JsonSerializer.Serialize(payload, InternalEventSerialization.Options) : null,
            Headers = headers is not null ? JsonSerializer.Serialize(headers, InternalEventSerialization.Options) : null,
            CorrelationId = correlationId,
            CausationId = causationId,
            Status = "Pending",
            AttemptCount = 0,
            AvailableAt = now,
            CreatedAt = now
        };

        await _ctx.InternalEventOutbox.AddAsync(entity, ct);
        await _ctx.SaveChangesAsync(ct);
        return eventId;
    }

    public async Task<List<InternalEventOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _ctx.InternalEventOutbox
            .Where(x => x.Status == "Pending" && x.AvailableAt <= now)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<List<InternalEventOutbox>> GetProcessingAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(timeout);
        return await _ctx.InternalEventOutbox
            .Where(x => x.Status == "Processing" && x.ProcessingStartedAt <= cutoff)
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<bool> TryAcquireAsync(long id, CancellationToken ct = default)
    {
        var row = await _ctx.InternalEventOutbox.FirstOrDefaultAsync(x => x.Id == id && x.Status == "Pending", ct);
        if (row is null) return false;
        row.Status = "Processing";
        row.ProcessingStartedAt = DateTimeOffset.UtcNow;
        row.AttemptCount++;
        await _ctx.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkProcessedAsync(long id, CancellationToken ct = default)
    {
        var row = await _ctx.InternalEventOutbox.FindAsync([id], ct);
        if (row is null) return;
        row.Status = "Processed";
        row.ProcessedAt = DateTimeOffset.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(long id, string failureCode, CancellationToken ct = default)
    {
        var row = await _ctx.InternalEventOutbox.FindAsync([id], ct);
        if (row is null) return;
        row.Status = "Failed";
        row.LastFailureCode = failureCode;
        row.LastFailureAt = DateTimeOffset.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task MarkRetryableAsync(long id, string failureCode, TimeSpan delay, CancellationToken ct = default)
    {
        var row = await _ctx.InternalEventOutbox.FindAsync([id], ct);
        if (row is null) return;
        row.Status = "Pending";
        row.AvailableAt = DateTimeOffset.UtcNow.Add(delay);
        row.LastFailureCode = failureCode;
        row.LastFailureAt = DateTimeOffset.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task RecoverStuckProcessingAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var stuck = await GetProcessingAsync(timeout, ct);
        foreach (var row in stuck)
        {
            row.Status = "Pending";
            row.AvailableAt = DateTimeOffset.UtcNow;
        }
        if (stuck.Count > 0)
            await _ctx.SaveChangesAsync(ct);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _ctx.InternalEventOutbox.CountAsync(x => x.Status == "Pending" && x.AvailableAt <= now, ct);
    }

    public async Task<DateTimeOffset?> GetOldestPendingAtAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _ctx.InternalEventOutbox
            .Where(x => x.Status == "Pending" && x.AvailableAt <= now)
            .MinAsync(x => (DateTimeOffset?)x.CreatedAt, ct);
    }

    public async Task<int> GetFailedCountAsync(CancellationToken ct = default)
    {
        return await _ctx.InternalEventOutbox.CountAsync(x => x.Status == "Failed", ct);
    }

    public async Task CleanupProcessedAsync(TimeSpan retention, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(retention);
        await _ctx.InternalEventOutbox
            .Where(x => x.Status == "Processed" && x.ProcessedAt <= cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public async Task CleanupFailedAsync(TimeSpan retention, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(retention);
        await _ctx.InternalEventOutbox
            .Where(x => x.Status == "Failed" && x.LastFailureAt <= cutoff)
            .ExecuteDeleteAsync(ct);
    }
}

internal static class InternalEventSerialization
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
