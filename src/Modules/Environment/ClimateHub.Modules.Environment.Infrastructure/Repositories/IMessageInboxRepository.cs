using ClimateHub.Modules.Environment.Infrastructure.Models;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public interface IMessageInboxRepository
{
    Task<bool> IsDuplicateAsync(string source, string messageId, CancellationToken cancellationToken = default);
    Task MarkProcessingAsync(string source, string messageId, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(string source, string messageId, string deviceId, string? bootId, long? sequenceNumber, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string source, string messageId, string errorCode, CancellationToken cancellationToken = default);
}