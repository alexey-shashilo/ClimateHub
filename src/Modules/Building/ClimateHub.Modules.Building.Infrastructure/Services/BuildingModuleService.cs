using ClimateHub.Modules.Building.Contracts;
using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Infrastructure.Services;

public class BuildingModuleService(
    IBuildingRepository buildingRepository,
    IFloorRepository floorRepository,
    IRoomRepository roomRepository) : IBuildingModule
{
    public async Task<bool> BuildingExistsAsync(BuildingId buildingId, CancellationToken cancellationToken = default)
    {
        return await buildingRepository.ExistsAsync(buildingId, cancellationToken);
    }

    public async Task<bool> FloorExistsAsync(FloorId floorId, CancellationToken cancellationToken = default)
    {
        return await floorRepository.ExistsAsync(floorId, cancellationToken);
    }

    public async Task<bool> RoomExistsAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        return await roomRepository.ExistsAsync(roomId, cancellationToken);
    }

    public async Task<BuildingInfo?> GetBuildingAsync(BuildingId buildingId, CancellationToken cancellationToken = default)
    {
        var building = await buildingRepository.GetByIdAsync(buildingId, cancellationToken);
        return building is null ? null : new BuildingInfo(building.Id, building.Name);
    }
}
