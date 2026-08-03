using ClimateHub.Modules.Environment.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public class MessageInboxRepository(EnvironmentDbContext context) : IMessageInboxRepository
{
    public async Task<bool> IsDuplicateAsync(string source, string messageId, CancellationToken cancellationToken = default)
    {
        return await context.MessageInbox
            .AnyAsync(e => e.Source == source && e.MessageIdStr == messageId, cancellationToken);
    }

    public async Task MarkProcessingAsync(string source, string messageId, CancellationToken cancellationToken = default)
    {
        var existing = await context.MessageInbox
            .FirstOrDefaultAsync(e => e.Source == source && e.MessageIdStr == messageId, cancellationToken);

        if (existing is not null && existing.Status == "Received")
        {
            existing.Status = "Processing";
            existing.ProcessingStartedAt = DateTimeOffset.UtcNow;
            existing.AttemptCount++;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkProcessedAsync(string source, string messageId, string deviceId, string? bootId, long? sequenceNumber, CancellationToken cancellationToken = default)
    {
        context.MessageInbox.Add(new MessageInboxEntity
        {
            Source = source,
            MessageIdStr = messageId,
            DeviceIdStr = deviceId,
            BootIdStr = bootId,
            SequenceNumber = sequenceNumber,
            ReceivedAt = DateTimeOffset.UtcNow,
            Status = "Processed",
            ProcessedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
        }
    }

    public async Task MarkFailedAsync(string source, string messageId, string errorCode, CancellationToken cancellationToken = default)
    {
        var existing = await context.MessageInbox
            .FirstOrDefaultAsync(e => e.Source == source && e.MessageIdStr == messageId, cancellationToken);

        if (existing is not null)
        {
            existing.Status = "Failed";
            existing.ErrorCode = errorCode;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}