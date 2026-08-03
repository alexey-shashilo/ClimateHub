using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.Domain.Aggregates;

public class Building : Entity<BuildingId>, IAggregateRoot
{
    private readonly List<Floor> _floors = [];

    public string Name { get; private set; }
    public string? Address { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public uint ConcurrencyToken { get; private set; }

    public IReadOnlyCollection<Floor> Floors => _floors.AsReadOnly();

    private Building(BuildingId id, string name, string? address)
    {
        Id = id;
        Name = name;
        Address = address;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        ConcurrencyToken = 0;
    }

    public static Building Create(string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Building name cannot be empty", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Building name cannot exceed 200 characters", nameof(name));

        var building = new Building(BuildingId.New(), name.Trim(), address?.Trim());
        building.RaiseDomainEvent(new BuildingCreatedEvent(building.Id, building.Name));
        return building;
    }

    public Floor AddFloor(string name, int level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name cannot be empty", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Floor name cannot exceed 200 characters", nameof(name));

        if (_floors.Any(f => f.Level == level))
            throw new InvalidOperationException($"Floor with level {level} already exists in building {Id}");

        var floor = Floor.Create(Id, name.Trim(), level);
        _floors.Add(floor);
        UpdatedAt = DateTimeOffset.UtcNow;
        ConcurrencyToken++;
        return floor;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Building name cannot be empty", nameof(newName));
        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        ConcurrencyToken++;
    }

    public void RemoveFloor(FloorId floorId)
    {
        var floor = _floors.FirstOrDefault(f => f.Id == floorId);
        if (floor is null)
            throw new KeyNotFoundException($"Floor {floorId} not found in building {Id}");
        _floors.Remove(floor);
        UpdatedAt = DateTimeOffset.UtcNow;
        ConcurrencyToken++;
    }
}

public record BuildingCreatedEvent(BuildingId BuildingId, string Name) : SharedKernel.Domain.DomainEvent;