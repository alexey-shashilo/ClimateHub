using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Domain.Aggregates;

public class Floor : Entity<FloorId>
{
    private readonly List<Room> _rooms = [];

    public BuildingId BuildingId { get; private init; }
    public string Name { get; private set; }
    public int Level { get; private init; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private Floor(FloorId id, BuildingId buildingId, string name, int level)
    {
        Id = id;
        BuildingId = buildingId;
        Name = name;
        Level = level;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Floor Create(BuildingId buildingId, string name, int level)
    {
        var floor = new Floor(FloorId.New(), buildingId, name.Trim(), level);
        floor.RaiseDomainEvent(new FloorCreatedEvent(floor.Id, floor.BuildingId, floor.Name));
        return floor;
    }

    public Room AddRoom(string name, string? purpose = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Room name cannot be empty", nameof(name));

        var room = Room.Create(Id, BuildingId, name.Trim(), purpose);
        _rooms.Add(room);
        UpdatedAt = DateTimeOffset.UtcNow;
        return room;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Floor name cannot be empty", nameof(newName));
        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveRoom(RoomId roomId)
    {
        var room = _rooms.FirstOrDefault(r => r.Id == roomId);
        if (room is null)
            throw new KeyNotFoundException($"Room {roomId} not found in floor {Id}");
        _rooms.Remove(room);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public record FloorCreatedEvent(FloorId FloorId, BuildingId BuildingId, string Name) : DomainEvent;