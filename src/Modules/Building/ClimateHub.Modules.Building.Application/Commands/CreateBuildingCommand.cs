using ClimateHub.Modules.Building.Domain.Repositories;

namespace ClimateHub.Modules.Building.Application.Commands;

public record CreateBuildingCommand
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}

public class CreateBuildingHandler(IBuildingRepository buildingRepository)
{
    public async Task<BuildingDto> HandleAsync(CreateBuildingCommand command, CancellationToken ct = default)
    {
        var building = Domain.Aggregates.Building.Create(command.Name, command.Address);
        await buildingRepository.AddAsync(building, ct);
        return BuildingDto.From(building);
    }
}
