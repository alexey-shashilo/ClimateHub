using ClimateHub.Modules.Climate.Domain;
using ClimateHub.Modules.Climate.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Climate.Infrastructure.Repositories;

public class ClimateEventInboxRepository : IClimateEventInboxRepository
{
    private readonly ClimateDbContext _db;

    public ClimateEventInboxRepository(ClimateDbContext db) { _db = db; }

    public async Task AddAsync(ClimateEventInboxEntry entry, CancellationToken ct = default)
    {
        var exists = await _db.ClimateEventInbox
            .AnyAsync(e => e.ConsumerName == entry.ConsumerName && e.EventId == entry.EventId, ct);
        if (!exists)
        {
            await _db.ClimateEventInbox.AddAsync(entry, ct);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(ClimateEventInboxEntry entry, CancellationToken ct = default)
    {
        _db.ClimateEventInbox.Update(entry);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<ClimateEventInboxEntry>> GetPendingAsync(string consumerName, int batchSize, CancellationToken ct = default)
        => await _db.ClimateEventInbox
            .Where(e => e.ConsumerName == consumerName && e.Status == "Pending")
            .OrderBy(e => e.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<ClimateEventInboxEntry>> GetStuckProcessingAsync(string consumerName, TimeSpan timeout, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(timeout);
        return await _db.ClimateEventInbox
            .Where(e => e.ConsumerName == consumerName && e.Status == "Processing" && e.ProcessingStartedAt < cutoff)
            .ToListAsync(ct);
    }
}