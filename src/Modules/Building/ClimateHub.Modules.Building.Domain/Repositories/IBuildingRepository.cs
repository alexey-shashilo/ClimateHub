using ClimateHub.Modules.Building.Domain.Aggregates;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Domain.Repositories;

public interface IBuildingRepository : IRepository<Aggregates.Building, BuildingId>
{
    Task<Aggregates.Building?> GetWithFloorsAsync(BuildingId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(BuildingId id, CancellationToken cancellationToken = default);
    Task<Aggregates.Room?> GetByRoomIdAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Aggregates.Building>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdateAndSaveAsync(Aggregates.Building entity, CancellationToken cancellationToken = default);
    Task RemoveAndSaveAsync(Aggregates.Building entity, CancellationToken cancellationToken = default);
}
