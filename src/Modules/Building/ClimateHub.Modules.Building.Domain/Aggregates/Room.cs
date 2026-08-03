using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Domain.Aggregates;

public class Room : Entity<RoomId>
{
    public FloorId FloorId { get; private init; }
    public BuildingId BuildingId { get; private init; }
    public string Name { get; private set; }
    public string? Purpose { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Room(RoomId id, FloorId floorId, BuildingId buildingId, string name, string? purpose)
    {
        Id = id;
        FloorId = floorId;
        BuildingId = buildingId;
        Name = name;
        Purpose = purpose;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Room Create(FloorId floorId, BuildingId buildingId, string name, string? purpose = null)
    {
        var room = new Room(RoomId.New(), floorId, buildingId, name.Trim(), purpose?.Trim());
        room.RaiseDomainEvent(new RoomCreatedEvent(room.Id, room.FloorId, room.BuildingId, room.Name));
        return room;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Room name cannot be empty", nameof(newName));

        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public record RoomCreatedEvent(RoomId RoomId, FloorId FloorId, BuildingId BuildingId, string Name) : DomainEvent;