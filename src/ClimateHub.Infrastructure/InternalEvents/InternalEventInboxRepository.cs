using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Infrastructure.InternalEvents;

public class InternalEventInboxRepository
{
    private readonly InternalEventsDbContext _ctx;

    public InternalEventInboxRepository(InternalEventsDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<bool> IsDuplicateAsync(string consumerName, Guid eventId, CancellationToken ct = default)
    {
        return await _ctx.InternalEventInbox.AnyAsync(
            x => x.ConsumerName == consumerName && x.EventId == eventId, ct);
    }

    public async Task<bool> TryAcquireAsync(string consumerName, Guid eventId, CancellationToken ct = default)
    {
        try
        {
            var entity = new InternalEventInbox
            {
                ConsumerName = consumerName,
                EventId = eventId,
                EventType = string.Empty,
                ReceivedAt = DateTimeOffset.UtcNow,
                Status = "Processing",
                ProcessingStartedAt = DateTimeOffset.UtcNow
            };
            await _ctx.InternalEventInbox.AddAsync(entity, ct);
            await _ctx.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task MarkProcessedAsync(string consumerName, Guid eventId, string eventType, CancellationToken ct = default)
    {
        var row = await _ctx.InternalEventInbox
            .FirstOrDefaultAsync(x => x.ConsumerName == consumerName && x.EventId == eventId, ct);
        if (row is null) return;
        row.EventType = eventType;
        row.Status = "Processed";
        row.ProcessedAt = DateTimeOffset.UtcNow;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(string consumerName, Guid eventId, string failureCode, CancellationToken ct = default)
    {
        var row = await _ctx.InternalEventInbox
            .FirstOrDefaultAsync(x => x.ConsumerName == consumerName && x.EventId == eventId, ct);
        if (row is null) return;
        row.Status = "Failed";
        row.FailureCode = failureCode;
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task<int> GetPendingCountAsync(string consumerName, CancellationToken ct = default)
    {
        return await _ctx.InternalEventInbox
            .CountAsync(x => x.ConsumerName == consumerName && x.Status == "Processing", ct);
    }

    public async Task<int> GetFailedCountAsync(string consumerName, CancellationToken ct = default)
    {
        return await _ctx.InternalEventInbox
            .CountAsync(x => x.ConsumerName == consumerName && x.Status == "Failed", ct);
    }

    public async Task<DateTimeOffset?> GetLastProcessedAtAsync(string consumerName, CancellationToken ct = default)
    {
        return await _ctx.InternalEventInbox
            .Where(x => x.ConsumerName == consumerName && x.Status == "Processed")
            .MaxAsync(x => (DateTimeOffset?)x.ProcessedAt, ct);
    }

    public async Task CleanupProcessedAsync(TimeSpan retention, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(retention);
        await _ctx.InternalEventInbox
            .Where(x => x.Status == "Processed" && x.ProcessedAt <= cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
