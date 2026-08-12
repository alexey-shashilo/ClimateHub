using ClimateHub.Modules.Commands.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Commands.Domain.Repositories;

public interface ICommandRepository
{
    Task<Command?> GetByIdAsync(CommandId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommandSummaryDto>> GetByDeviceAsync(DeviceId deviceId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommandSummaryDto>> GetByRoomAsync(RoomId roomId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommandSummaryDto>> GetRecentAsync(int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyCollection<Command>> GetStaleAsync(DateTimeOffset olderThan, CancellationToken ct = default);
    Task<IReadOnlyCollection<Command>> GetStuckQueuedAsync(DateTimeOffset olderThan, CancellationToken ct = default);
    Task AddAsync(Command command, CancellationToken ct = default);
    Task UpdateAsync(Command command, CancellationToken ct = default);
}

public interface ICommandOutboxRepository
{
    Task AddAsync(CommandOutbox item, CancellationToken ct = default);
    Task<IReadOnlyCollection<CommandOutbox>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task<bool> TryAcquireAsync(long id, CancellationToken ct = default);
    Task MarkPublishedAsync(long id, CancellationToken ct = default);
    Task MarkFailedAsync(long id, string errorCode, CancellationToken ct = default);
    Task MarkRetryableAsync(long id, string errorCode, TimeSpan delay, CancellationToken ct = default);
    Task RecoverStuckAsync(TimeSpan timeout, CancellationToken ct = default);
}

public interface ICommandInboxRepository
{
    Task<bool> IsDuplicateAsync(string source, string messageId, CancellationToken ct = default);
    Task MarkProcessedAsync(string source, string messageId, CommandId commandId, string messageType, CancellationToken ct = default);
}

public interface IDeviceCapabilityStateRepository
{
    Task<DeviceCapabilityState?> GetAsync(DeviceId deviceId, string capabilityCode, CancellationToken ct = default);
    Task UpsertDesiredAsync(DeviceId deviceId, string capabilityCode, string desiredValue, CommandId commandId, CancellationToken ct = default);
    Task UpsertReportedAsync(DeviceId deviceId, string capabilityCode, string reportedValue, CommandId commandId, CancellationToken ct = default);
}
