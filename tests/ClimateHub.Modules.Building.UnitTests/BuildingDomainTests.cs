using BuildingAggregate = ClimateHub.Modules.Building.Domain.Aggregates.Building;
using ClimateHub.Modules.Building.Domain.Aggregates;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Building.UnitTests;

public class BuildingDomainTests
{
    [Fact]
    public void CreateBuilding_WithValidName_Succeeds()
    {
        var building = BuildingAggregate.Create("Main Building");

        Assert.NotEqual(Guid.Empty, building.Id.Value);
        Assert.Equal("Main Building", building.Name);
        Assert.Null(building.Address);
        Assert.Empty(building.Floors);
        Assert.Single(building.DomainEvents);
        Assert.IsType<BuildingCreatedEvent>(building.DomainEvents.First());
    }

    [Fact]
    public void CreateBuilding_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => BuildingAggregate.Create(""));
        Assert.Throws<ArgumentException>(() => BuildingAggregate.Create("   "));
    }

    [Fact]
    public void CreateBuilding_WithAddress_SetsAddress()
    {
        var building = BuildingAggregate.Create("Building", "123 Main St");
        Assert.Equal("123 Main St", building.Address);
    }

    [Fact]
    public void AddFloor_ToBuilding_Succeeds()
    {
        var building = BuildingAggregate.Create("Test Building");
        var floor = building.AddFloor("First Floor", 1);

        Assert.NotEqual(Guid.Empty, floor.Id.Value);
        Assert.Equal("First Floor", floor.Name);
        Assert.Equal(1, floor.Level);
        Assert.Equal(building.Id, floor.BuildingId);
        Assert.Single(building.Floors);
        Assert.Empty(floor.Rooms);
        Assert.Single(floor.DomainEvents);
    }

    [Fact]
    public void AddFloor_DuplicateLevel_Throws()
    {
        var building = BuildingAggregate.Create("Test Building");
        building.AddFloor("First", 1);

        Assert.Throws<InvalidOperationException>(() => building.AddFloor("Second", 1));
    }

    [Fact]
    public void AddFloor_EmptyName_Throws()
    {
        var building = BuildingAggregate.Create("Test Building");
        Assert.Throws<ArgumentException>(() => building.AddFloor("", 1));
    }

    [Fact]
    public void AddRoom_ToFloor_Succeeds()
    {
        var building = BuildingAggregate.Create("Test Building");
        var floor = building.AddFloor("First", 1);
        var room = floor.AddRoom("Living Room");

        Assert.NotEqual(Guid.Empty, room.Id.Value);
        Assert.Equal("Living Room", room.Name);
        Assert.Equal(floor.Id, room.FloorId);
        Assert.Equal(building.Id, room.BuildingId);
        Assert.Single(floor.Rooms);
        Assert.Single(room.DomainEvents);
    }

    [Fact]
    public void AddRoom_EmptyName_Throws()
    {
        var building = BuildingAggregate.Create("Test Building");
        var floor = building.AddFloor("First", 1);
        Assert.Throws<ArgumentException>(() => floor.AddRoom(""));
    }

    [Fact]
    public void AddRoom_DuplicateName_Allowed()
    {
        var building = BuildingAggregate.Create("Test Building");
        var floor = building.AddFloor("First", 1);
        floor.AddRoom("Room");
        floor.AddRoom("Room");

        Assert.Equal(2, floor.Rooms.Count);
    }

    [Fact]
    public void Building_WithMultipleFloors_Succeeds()
    {
        var building = BuildingAggregate.Create("Multi-floor Building");
        building.AddFloor("Ground", 0);
        building.AddFloor("First", 1);
        building.AddFloor("Second", 2);

        Assert.Equal(3, building.Floors.Count);
    }

    [Fact]
    public void Floor_WithMultipleRooms_Succeeds()
    {
        var building = BuildingAggregate.Create("Building");
        var floor = building.AddFloor("First", 1);
        floor.AddRoom("Kitchen");
        floor.AddRoom("Bedroom");
        floor.AddRoom("Bathroom");

        Assert.Equal(3, floor.Rooms.Count);
    }

    [Fact]
    public void Room_Rename_UpdatesName()
    {
        var building = BuildingAggregate.Create("Building");
        var floor = building.AddFloor("First", 1);
        var room = floor.AddRoom("Old Name");

        room.Rename("New Name");

        Assert.Equal("New Name", room.Name);
    }

    [Fact]
    public void ClearDomainEvents_RemovesEvents()
    {
        var building = BuildingAggregate.Create("Test");
        building.ClearDomainEvents();
        Assert.Empty(building.DomainEvents);
    }

    [Fact]
    public void BuildingId_ToString_ReturnsFormattedGuid()
    {
        var id = BuildingId.New();
        var str = id.ToString();
        Assert.Matches(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", str);
    }
}