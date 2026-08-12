using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Application.Commands;

public record CreateRoomCommand
{
    public FloorId FloorId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Purpose { get; init; }
}

public class CreateRoomHandler(
    IFloorRepository floorRepository,
    IRoomRepository roomRepository)
{
    public async Task<RoomDto> HandleAsync(CreateRoomCommand command, CancellationToken ct = default)
    {
        var floor = await floorRepository.GetWithRoomsAsync(command.FloorId, ct);
        if (floor is null)
            throw new KeyNotFoundException($"Floor {command.FloorId} not found");

        var room = floor.AddRoom(command.Name);
        await roomRepository.AddAsync(room, ct);

        return RoomDto.From(room);
    }
}
