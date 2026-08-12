using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Commands.Infrastructure.Repositories;

public class CommandRepository(CommandsDbContext context) : ICommandRepository
{
    public async Task<Command?> GetByIdAsync(CommandId id, CancellationToken ct = default)
        => await context.Commands.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyCollection<CommandSummaryDto>> GetByDeviceAsync(DeviceId deviceId, int limit = 50, CancellationToken ct = default)
        => await context.Commands.Where(c => c.DeviceId == deviceId).OrderByDescending(c => c.CreatedAt).Take(limit)
            .Select(c => new CommandSummaryDto(c.Id, c.Status.ToString(), c.DeviceId, c.CapabilityCode, c.Operation, c.ParametersJson, c.CreatedAt, c.CompletedAt, c.LastErrorCode))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<CommandSummaryDto>> GetByRoomAsync(RoomId roomId, int limit = 50, CancellationToken ct = default)
        => await context.Commands.Where(c => c.RoomId == roomId).OrderByDescending(c => c.CreatedAt).Take(limit)
            .Select(c => new CommandSummaryDto(c.Id, c.Status.ToString(), c.DeviceId, c.CapabilityCode, c.Operation, c.ParametersJson, c.CreatedAt, c.CompletedAt, c.LastErrorCode))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<CommandSummaryDto>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
        => await context.Commands.OrderByDescending(c => c.CreatedAt).Take(limit)
            .Select(c => new CommandSummaryDto(c.Id, c.Status.ToString(), c.DeviceId, c.CapabilityCode, c.Operation, c.ParametersJson, c.CreatedAt, c.CompletedAt, c.LastErrorCode))
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<Command>> GetStaleAsync(DateTimeOffset olderThan, CancellationToken ct = default)
        => await context.Commands
            .Where(c => (c.Status == CommandStatus.Published || c.Status == CommandStatus.Acknowledged || c.Status == CommandStatus.Executing)
                && c.CreatedAt <= olderThan)
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<Command>> GetStuckQueuedAsync(DateTimeOffset olderThan, CancellationToken ct = default)
        => await context.Commands
            .Where(c => c.Status == CommandStatus.Queued && c.CreatedAt <= olderThan)
            .ToListAsync(ct);

    public async Task AddAsync(Command command, CancellationToken ct = default)
    { await context.Commands.AddAsync(command, ct); await context.SaveChangesAsync(ct); }

    public async Task UpdateAsync(Command command, CancellationToken ct = default)
    { context.Commands.Update(command); await context.SaveChangesAsync(ct); }
}

public class CommandOutboxRepository(CommandsDbContext context) : ICommandOutboxRepository
{
    public async Task AddAsync(CommandOutbox item, CancellationToken ct = default)
    { await context.Outbox.AddAsync(item, ct); await context.SaveChangesAsync(ct); }

    public async Task<IReadOnlyCollection<CommandOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default)
        => await context.Outbox.Where(o => o.Status == "Pending" || (o.Status == "Retrying" && o.AvailableAt <= DateTimeOffset.UtcNow))
            .OrderBy(o => o.CreatedAt).Take(batchSize).ToListAsync(ct);

    public async Task<bool> TryAcquireAsync(long id, CancellationToken ct = default)
    {
        var item = await context.Outbox.FindAsync([id, ct], ct);
        if (item is null || item.Status == "Published") return false;
        item.Status = "Processing";
        item.ProcessingStartedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkPublishedAsync(long id, CancellationToken ct = default)
    {
        var item = await context.Outbox.FindAsync([id, ct], ct);
        if (item is not null) { item.Status = "Published"; item.PublishedAt = DateTimeOffset.UtcNow; await context.SaveChangesAsync(ct); }
    }

    public async Task MarkFailedAsync(long id, string errorCode, CancellationToken ct = default)
    {
        var item = await context.Outbox.FindAsync([id, ct], ct);
        if (item is not null) { item.Status = "Failed"; item.LastFailureCode = errorCode; item.LastFailureAt = DateTimeOffset.UtcNow; await context.SaveChangesAsync(ct); }
    }

    public async Task MarkRetryableAsync(long id, string errorCode, TimeSpan delay, CancellationToken ct = default)
    {
        var item = await context.Outbox.FindAsync([id, ct], ct);
        if (item is not null) { item.Status = "Retrying"; item.AttemptCount++; item.AvailableAt = DateTimeOffset.UtcNow.Add(delay); item.LastFailureCode = errorCode; await context.SaveChangesAsync(ct); }
    }

    public async Task RecoverStuckAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(timeout);
        var stuck = await context.Outbox.Where(o => o.Status == "Processing" && o.ProcessingStartedAt <= cutoff).ToListAsync(ct);
        foreach (var s in stuck) { s.Status = "Pending"; s.AttemptCount++; }
        if (stuck.Count > 0) await context.SaveChangesAsync(ct);
    }
}

public class CommandInboxRepository(CommandsDbContext context) : ICommandInboxRepository
{
    public async Task<bool> IsDuplicateAsync(string source, string messageId, CancellationToken ct = default)
        => await context.Inbox.AnyAsync(i => i.Source == source && i.MessageId == messageId, ct);

    public async Task MarkProcessedAsync(string source, string messageId, CommandId commandId, string messageType, CancellationToken ct = default)
    {
        context.Inbox.Add(new CommandInboxMessage { Source = source, MessageId = messageId, CommandId = commandId, MessageType = messageType, ReceivedAt = DateTimeOffset.UtcNow, Status = "Processed", ProcessedAt = DateTimeOffset.UtcNow });
        try { await context.SaveChangesAsync(ct); } catch (DbUpdateException) { }
    }
}

public class DeviceCapabilityStateRepository(CommandsDbContext context) : IDeviceCapabilityStateRepository
{
    public async Task<DeviceCapabilityState?> GetAsync(DeviceId deviceId, string capabilityCode, CancellationToken ct = default)
        => await context.CapabilityStates.FirstOrDefaultAsync(s => s.DeviceId == deviceId && s.CapabilityCode == capabilityCode, ct);

    public async Task UpsertDesiredAsync(DeviceId deviceId, string capabilityCode, string desiredValue, CommandId commandId, CancellationToken ct = default)
    {
        var existing = await GetAsync(deviceId, capabilityCode, ct);
        if (existing is null)
        { context.CapabilityStates.Add(new DeviceCapabilityState { DeviceId = deviceId, CapabilityCode = capabilityCode, DesiredValue = desiredValue, DesiredAt = DateTimeOffset.UtcNow, DesiredByCommandId = commandId, Quality = "desired", Version = 1 }); }
        else
        { existing.DesiredValue = desiredValue; existing.DesiredAt = DateTimeOffset.UtcNow; existing.DesiredByCommandId = commandId; existing.Version++; }
        await context.SaveChangesAsync(ct);
    }

    public async Task UpsertReportedAsync(DeviceId deviceId, string capabilityCode, string reportedValue, CommandId commandId, CancellationToken ct = default)
    {
        var existing = await GetAsync(deviceId, capabilityCode, ct);
        if (existing is null)
        { context.CapabilityStates.Add(new DeviceCapabilityState { DeviceId = deviceId, CapabilityCode = capabilityCode, ReportedValue = reportedValue, ReportedAt = DateTimeOffset.UtcNow, ReportedByCommandId = commandId, Quality = "reported", Version = 1 }); }
        else
        { existing.ReportedValue = reportedValue; existing.ReportedAt = DateTimeOffset.UtcNow; existing.ReportedByCommandId = commandId; existing.Quality = "reported"; existing.Version++; }
        await context.SaveChangesAsync(ct);
    }
}
