using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public interface IRoomEnvironmentStateRepository
{
    Task<IReadOnlyCollection<RoomParameterEntity>> GetParametersAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task UpsertParameterAsync(RoomParameterEntity entity, CancellationToken cancellationToken = default);
}
