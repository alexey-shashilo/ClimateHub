namespace ClimateHub.Modules.Building.Application.Commands;

public record BuildingDto(
    string Id,
    string Name,
    string? Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<FloorDto> Floors)
{
    public static BuildingDto From(Domain.Aggregates.Building building) => new(
        building.Id.ToString(),
        building.Name,
        building.Address,
        building.CreatedAt,
        building.UpdatedAt,
        building.Floors.Select(FloorDto.From).ToList());
}

public record FloorDto(
    string Id,
    string BuildingId,
    string Name,
    int Level,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<RoomDto> Rooms)
{
    public static FloorDto From(Domain.Aggregates.Floor floor) => new(
        floor.Id.ToString(),
        floor.BuildingId.ToString(),
        floor.Name,
        floor.Level,
        floor.CreatedAt,
        floor.UpdatedAt,
        floor.Rooms.Select(RoomDto.From).ToList());
}

public record RoomDto(
    string Id,
    string FloorId,
    string BuildingId,
    string Name,
    string? Purpose,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static RoomDto From(Domain.Aggregates.Room room) => new(
        room.Id.ToString(),
        room.FloorId.ToString(),
        room.BuildingId.ToString(),
        room.Name,
        room.Purpose,
        room.CreatedAt,
        room.UpdatedAt);
}