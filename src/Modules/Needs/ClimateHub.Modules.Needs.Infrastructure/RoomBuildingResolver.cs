using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.Modules.Needs.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class RoomBuildingResolver : IRoomBuildingResolver
{
    private readonly IRoomRepository _roomRepository;

    public RoomBuildingResolver(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<BuildingId?> ResolveBuildingIdAsync(RoomId roomId, CancellationToken ct = default)
    {
        var room = await _roomRepository.GetByIdAsync(roomId, ct);
        return room?.BuildingId;
    }
}