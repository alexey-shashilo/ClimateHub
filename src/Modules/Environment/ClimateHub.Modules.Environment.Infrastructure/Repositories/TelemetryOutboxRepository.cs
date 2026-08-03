using ClimateHub.Modules.Environment.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public class TelemetryOutboxRepository(EnvironmentDbContext context) : ITelemetryOutboxRepository
{
    public async Task CreateAsync(TelemetryOutboxEntity entity, CancellationToken cancellationToken = default)
    {
        await context.TelemetryOutbox.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<TelemetryOutboxEntity>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await context.TelemetryOutbox
            .Where(o => o.Status == "Pending" || (o.Status == "Retrying" && o.AvailableAt <= DateTimeOffset.UtcNow))
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await context.TelemetryOutbox.FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
        if (entity is not null)
        {
            entity.Status = "Processed";
            entity.ProcessedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkFailedAsync(long id, string errorCode, CancellationToken cancellationToken = default)
    {
        var entity = await context.TelemetryOutbox.FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
        if (entity is not null)
        {
            entity.Status = "Failed";
            entity.AttemptCount++;
            entity.ErrorCode = errorCode;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkRetryableAsync(long id, string errorCode, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        var entity = await context.TelemetryOutbox.FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
        if (entity is not null)
        {
            entity.Status = "Retrying";
            entity.AttemptCount++;
            entity.AvailableAt = DateTimeOffset.UtcNow.Add(delay);
            entity.ErrorCode = errorCode;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> TryAcquireAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await context.TelemetryOutbox.FindAsync([id, cancellationToken], cancellationToken: cancellationToken);
        if (entity is null || entity.Status == "Processed") return false;
        entity.Status = "Processing";
        entity.ProcessingStartedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RecoverStuckAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(timeout);
        var stuck = await context.TelemetryOutbox
            .Where(o => o.Status == "Processing" && o.ProcessingStartedAt <= cutoff)
            .ToListAsync(cancellationToken);
        foreach (var item in stuck)
        {
            item.Status = "Pending";
            item.AttemptCount++;
        }
        if (stuck.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}