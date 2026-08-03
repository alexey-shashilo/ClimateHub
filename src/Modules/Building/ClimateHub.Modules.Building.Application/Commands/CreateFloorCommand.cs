using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Application.Commands;

public record CreateFloorCommand
{
    public BuildingId BuildingId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Level { get; init; }
}

public class CreateFloorHandler(
    IBuildingRepository buildingRepository,
    IFloorRepository floorRepository)
{
    public async Task<FloorDto> HandleAsync(CreateFloorCommand command, CancellationToken ct = default)
    {
        var building = await buildingRepository.GetWithFloorsAsync(command.BuildingId, ct);
        if (building is null)
            throw new KeyNotFoundException($"Building {command.BuildingId} not found");

        var floor = building.AddFloor(command.Name, command.Level);
        await floorRepository.AddAsync(floor, ct);

        return FloorDto.From(floor);
    }
}