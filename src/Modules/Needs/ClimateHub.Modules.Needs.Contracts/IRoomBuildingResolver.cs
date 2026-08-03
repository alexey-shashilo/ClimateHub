using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Contracts;

public interface IRoomBuildingResolver
{
    Task<BuildingId?> ResolveBuildingIdAsync(RoomId roomId, CancellationToken ct = default);
}