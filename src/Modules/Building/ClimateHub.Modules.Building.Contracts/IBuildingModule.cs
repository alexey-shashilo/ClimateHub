using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Contracts;

public interface IBuildingModule
{
    Task<bool> BuildingExistsAsync(BuildingId buildingId, CancellationToken cancellationToken = default);
    Task<bool> FloorExistsAsync(FloorId floorId, CancellationToken cancellationToken = default);
    Task<bool> RoomExistsAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<BuildingInfo?> GetBuildingAsync(BuildingId buildingId, CancellationToken cancellationToken = default);
}

public record BuildingInfo(BuildingId Id, string Name);
public record FloorInfo(FloorId Id, string Name, int Level);
public record RoomInfo(RoomId Id, string Name, FloorId FloorId);