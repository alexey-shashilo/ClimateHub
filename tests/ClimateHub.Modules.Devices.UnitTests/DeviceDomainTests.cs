using DeviceAggregate = ClimateHub.Modules.Devices.Domain.Aggregates.Device;
using ClimateHub.Modules.Devices.Domain.Aggregates;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.UnitTests;

public class DeviceDomainTests
{
    private static readonly DeviceModel TestModel = new("Acme", "Sensor-2000", "1.0");
    private const string TestHardwareId = "SN-001";
    private const string TestProtocol = "1.0";

    [Fact]
    public void RegisterDevice_WithValidData_Succeeds()
    {
        var device = DeviceAggregate.Register(TestHardwareId, "Living Room Sensor", TestModel, TestProtocol);

        Assert.NotEqual(Guid.Empty, device.Id.Value);
        Assert.Equal(TestHardwareId, device.HardwareId);
        Assert.Equal("Living Room Sensor", device.Name);
        Assert.Equal("Acme", device.Model.Manufacturer);
        Assert.Equal("Sensor-2000", device.Model.ModelName);
        Assert.Equal(DeviceStatus.Registered, device.Status);
        Assert.Empty(device.Capabilities);
        Assert.Null(device.ActiveAssignment);
        Assert.Single(device.DomainEvents);
        Assert.IsType<DeviceRegisteredEvent>(device.DomainEvents.First());
    }

    [Fact]
    public void RegisterDevice_WithEmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => DeviceAggregate.Register("HW1", "", TestModel, TestProtocol));
        Assert.Throws<ArgumentException>(() => DeviceAggregate.Register("HW2", "   ", TestModel, TestProtocol));
    }

    [Fact]
    public void RegisterDevice_WithEmptyHardwareId_Throws()
    {
        Assert.Throws<ArgumentException>(() => DeviceAggregate.Register("", "Device", TestModel, TestProtocol));
    }

    [Fact]
    public void AddCapability_ToDevice_Succeeds()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        var capability = DeviceCapability.Temperature(device.Id);

        device.AddCapability(capability);

        Assert.Single(device.Capabilities);
        Assert.True(device.HasCapability("measure.temperature"));
    }

    [Fact]
    public void AddCapability_DuplicateCode_Throws()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        device.AddCapability(DeviceCapability.Temperature(device.Id));

        Assert.Throws<InvalidOperationException>(() =>
            device.AddCapability(DeviceCapability.Temperature(device.Id)));
    }

    [Fact]
    public void HasCapability_ReturnsFalse_WhenNotPresent()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        Assert.False(device.HasCapability("measure.co2"));
    }

    [Fact]
    public void AssignDevice_ToRoom_Succeeds()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        var roomId = RoomId.New();

        device.AssignToRoom(roomId);

        Assert.NotNull(device.ActiveAssignment);
        Assert.Equal(roomId, device.ActiveAssignment.RoomId);
        Assert.Equal(AssignmentStatus.Active, device.ActiveAssignment.Status);
        Assert.Single(device.DomainEvents.OfType<DeviceAssignedEvent>());
        Assert.Equal(DeviceStatus.Active, device.Status);
    }

    [Fact]
    public void AssignDevice_SameRoom_Idempotent()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        var roomId = RoomId.New();

        device.AssignToRoom(roomId);
        device.AssignToRoom(roomId);

        Assert.Single(device.DomainEvents.OfType<DeviceAssignedEvent>());
    }

    [Fact]
    public void Assign_RoomChanged_CompletesPrevious()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        var roomA = RoomId.New();
        var roomB = RoomId.New();

        device.AssignToRoom(roomA);
        device.AssignToRoom(roomB);

        Assert.Equal(roomB, device.ActiveAssignment!.RoomId);
        Assert.Equal(2, device.AssignmentHistory.Count);
        Assert.Equal(AssignmentStatus.Completed, device.AssignmentHistory.First().Status);
    }

    [Fact]
    public void RecordHeartbeat_UpdatesBootIdAndSequence()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        var bootId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        device.RecordHeartbeat(bootId, 42, now);

        Assert.Equal(bootId, device.CurrentBootId);
        Assert.Equal(42, device.LastSequenceNumber);
        Assert.Equal(DeviceStatus.Active, device.Status);
    }

    [Fact]
    public void Activate_ChangesStatus()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);

        device.Activate();

        Assert.Equal(DeviceStatus.Active, device.Status);
    }

    [Fact]
    public void Deactivate_ChangesStatus()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        device.Activate();

        device.Deactivate();

        Assert.Equal(DeviceStatus.Inactive, device.Status);
    }

    [Fact]
    public void ClearDomainEvents_RemovesEvents()
    {
        var device = DeviceAggregate.Register("HW1", "Device", TestModel, TestProtocol);
        device.ClearDomainEvents();
        Assert.Empty(device.DomainEvents);
    }

    [Fact]
    public void DeviceModel_Equality()
    {
        var model1 = new DeviceModel("Acme", "Sensor-2000");
        var model2 = new DeviceModel("Acme", "Sensor-2000");
        var model3 = new DeviceModel("Other", "Sensor-2000");

        Assert.Equal(model1, model2);
        Assert.NotEqual(model1, model3);
    }

    [Fact]
    public void CreateCapability_StandardCodes()
    {
        var deviceId = DeviceId.New();

        var temp = DeviceCapability.Temperature(deviceId);
        Assert.Equal("measure.temperature", temp.Code);
        Assert.Equal("celsius", temp.Unit);
        Assert.Equal(-50, temp.MinValue);
        Assert.Equal(100, temp.MaxValue);
    }
}
