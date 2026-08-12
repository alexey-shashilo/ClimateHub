using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Application.Commands;

public record UpdateBuildingCommand { public BuildingId Id { get; init; } public string Name { get; init; } = ""; }
public class UpdateBuildingHandler(IBuildingRepository repo)
{
    public async Task HandleAsync(UpdateBuildingCommand cmd, CancellationToken ct = default)
    {
        var b = await repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) throw new KeyNotFoundException("BUILDING_NOT_FOUND");
        b.Rename(cmd.Name);
        await repo.UpdateAndSaveAsync(b, ct);
    }
}

public record DeleteBuildingCommand { public BuildingId Id { get; init; } }
public class DeleteBuildingHandler(IBuildingRepository repo)
{
    public async Task HandleAsync(DeleteBuildingCommand cmd, CancellationToken ct = default)
    {
        var b = await repo.GetByIdAsync(cmd.Id, ct);
        if (b is null) throw new KeyNotFoundException("BUILDING_NOT_FOUND");
        await repo.RemoveAndSaveAsync(b, ct);
    }
}

public record UpdateFloorCommand { public FloorId Id { get; init; } public string Name { get; init; } = ""; public int Level { get; init; } }
public class UpdateFloorHandler(IFloorRepository floorRepo)
{
    public async Task HandleAsync(UpdateFloorCommand cmd, CancellationToken ct = default)
    {
        var floor = await floorRepo.GetByIdAsync(cmd.Id, ct);
        if (floor is null) throw new KeyNotFoundException("FLOOR_NOT_FOUND");
        floor.Rename(cmd.Name);
        await floorRepo.UpdateAndSaveAsync(floor, ct);
    }
}

public record DeleteFloorCommand { public BuildingId BuildingId { get; init; } public FloorId FloorId { get; init; } }
public class DeleteFloorHandler(IBuildingRepository repo)
{
    public async Task HandleAsync(DeleteFloorCommand cmd, CancellationToken ct = default)
    {
        var b = await repo.GetByIdAsync(cmd.BuildingId, ct);
        if (b is null) throw new KeyNotFoundException("BUILDING_NOT_FOUND");
        b.RemoveFloor(cmd.FloorId);
        await repo.UpdateAndSaveAsync(b, ct);
    }
}

public record UpdateRoomCommand { public RoomId Id { get; init; } public string Name { get; init; } = ""; public string? Purpose { get; init; } }
public class UpdateRoomHandler(IRoomRepository roomRepo)
{
    public async Task HandleAsync(UpdateRoomCommand cmd, CancellationToken ct = default)
    {
        var room = await roomRepo.GetByIdAsync(cmd.Id, ct);
        if (room is null) throw new KeyNotFoundException("ROOM_NOT_FOUND");
        room.Rename(cmd.Name);
        await roomRepo.UpdateAndSaveAsync(room, ct);
    }
}

public record DeleteRoomCommand { public FloorId FloorId { get; init; } public RoomId RoomId { get; init; } }
public class DeleteRoomHandler(IFloorRepository floorRepo)
{
    public async Task HandleAsync(DeleteRoomCommand cmd, CancellationToken ct = default)
    {
        var floor = await floorRepo.GetByIdAsync(cmd.FloorId, ct);
        if (floor is null) throw new KeyNotFoundException("FLOOR_NOT_FOUND");
        floor.RemoveRoom(cmd.RoomId);
        await floorRepo.UpdateAndSaveAsync(floor, ct);
    }
}
