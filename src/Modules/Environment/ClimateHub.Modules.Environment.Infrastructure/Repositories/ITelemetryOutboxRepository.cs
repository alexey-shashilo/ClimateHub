using ClimateHub.Modules.Environment.Infrastructure.Models;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public interface ITelemetryOutboxRepository
{
    Task CreateAsync(TelemetryOutboxEntity entity, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TelemetryOutboxEntity>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(long id, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(long id, string errorCode, CancellationToken cancellationToken = default);
    Task RecoverStuckAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<bool> TryAcquireAsync(long id, CancellationToken cancellationToken = default);
    Task MarkRetryableAsync(long id, string errorCode, TimeSpan delay, CancellationToken cancellationToken = default);
}
