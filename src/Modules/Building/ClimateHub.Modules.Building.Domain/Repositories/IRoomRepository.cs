using ClimateHub.Modules.Building.Domain.Aggregates;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Domain.Repositories;

public interface IRoomRepository : IRepository<Aggregates.Room, RoomId>
{
    Task<bool> ExistsAsync(RoomId id, CancellationToken cancellationToken = default);
    Task UpdateAndSaveAsync(Aggregates.Room entity, CancellationToken cancellationToken = default);
    Task RemoveAndSaveAsync(Aggregates.Room entity, CancellationToken cancellationToken = default);
}