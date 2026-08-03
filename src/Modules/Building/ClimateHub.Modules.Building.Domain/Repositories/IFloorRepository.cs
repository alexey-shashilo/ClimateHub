using ClimateHub.Modules.Building.Domain.Aggregates;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Domain.Repositories;

public interface IFloorRepository : IRepository<Aggregates.Floor, FloorId>
{
    Task<Aggregates.Floor?> GetWithRoomsAsync(FloorId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(FloorId id, CancellationToken cancellationToken = default);
    Task UpdateAndSaveAsync(Floor entity, CancellationToken cancellationToken = default);
    Task RemoveAndSaveAsync(Floor entity, CancellationToken cancellationToken = default);
}