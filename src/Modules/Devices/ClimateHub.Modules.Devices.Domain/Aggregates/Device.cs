using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Domain.Aggregates;

public enum DeviceStatus
{
    Registered,
    Active,
    Inactive,
    Decommissioned
}

public class Device : Entity<DeviceId>, IAggregateRoot
{
    private List<DeviceCapability> _capabilities = [];
    internal List<DeviceAssignment> _assignmentHistory = [];
    public IReadOnlyCollection<DeviceAssignment> AssignmentHistoryReadOnly => _assignmentHistory.AsReadOnly();
    public ICollection<DeviceAssignment> AssignmentHistory { get => _assignmentHistory; set => _assignmentHistory = (List<DeviceAssignment>)(value ?? []); }

    public string HardwareId { get; private set; }
    public string Name { get; private set; }
    public DeviceModel Model { get; private set; }
    public string ProtocolVersion { get; private set; }
    public DeviceStatus Status { get; private set; }
    public DeviceConnectivityState ConnectivityState { get; private set; }
    public DateTimeOffset RegisteredAt { get; private init; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public Guid? CurrentBootId { get; private set; }
    public long LastSequenceNumber { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DeviceAssignment? ActiveAssignment =>
        _assignmentHistory?.LastOrDefault(a => a.Status == AssignmentStatus.Active) ?? null;

    public IReadOnlyCollection<DeviceCapability> Capabilities => _capabilities.AsReadOnly();

    private Device()
    {
        HardwareId = null!;
        Name = null!;
        Model = null!;
        ProtocolVersion = null!;
        _capabilities = [];
        _assignmentHistory = [];
    }

    private Device(DeviceId id, string hardwareId, string name, DeviceModel model, string protocolVersion)
    {
        _capabilities = [];
        _assignmentHistory = [];
        Id = id;
        HardwareId = hardwareId;
        Name = name;
        Model = model;
        ProtocolVersion = protocolVersion;
        Status = DeviceStatus.Registered;
        ConnectivityState = DeviceConnectivityState.Offline;
        var now = DateTimeOffset.UtcNow;
        RegisteredAt = now;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Device Register(string hardwareId, string name, DeviceModel model, string protocolVersion)
    {
        if (string.IsNullOrWhiteSpace(hardwareId))
            throw new ArgumentException("HardwareId cannot be empty", nameof(hardwareId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Device name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(protocolVersion))
            throw new ArgumentException("ProtocolVersion cannot be empty", nameof(protocolVersion));

        var device = new Device(DeviceId.New(), hardwareId.Trim(), name.Trim(), model, protocolVersion.Trim());
        device.RaiseDomainEvent(new DeviceRegisteredEvent(device.Id, device.Name, device.Model.ModelName));
        return device;
    }

    public void AddCapability(DeviceCapability capability)
    {
        if (_capabilities.Any(c => c.Code == capability.Code))
            throw new InvalidOperationException($"Capability '{capability.Code}' already registered for device {Id}");

        _capabilities.Add(capability);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasCapability(string code)
    {
        return _capabilities.Any(c => c.Code == code);
    }

    public DeviceAssignment AssignToRoom(RoomId roomId)
    {
        var current = ActiveAssignment;
        if (current?.RoomId == roomId)
            return current;

        if (current is not null)
        {
            current.Complete();
        }

        var assignment = DeviceAssignment.Create(Id, roomId);
        _assignmentHistory.Add(assignment);
        LastSeenAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (Status == DeviceStatus.Registered)
            Status = DeviceStatus.Active;

        RaiseDomainEvent(new DeviceAssignedEvent(Id, roomId));
        return assignment;
    }

    public void RecordHeartbeat(Guid bootId, long sequenceNumber, DateTimeOffset timestamp)
    {
        CurrentBootId = bootId;
        LastSequenceNumber = sequenceNumber;
        LastSeenAt = timestamp;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (Status == DeviceStatus.Registered)
            Status = DeviceStatus.Active;
    }

    public void Activate()
    {
        if (Status == DeviceStatus.Registered || Status == DeviceStatus.Inactive)
        {
            Status = DeviceStatus.Active;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Deactivate()
    {
        if (Status == DeviceStatus.Active)
        {
            Status = DeviceStatus.Inactive;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void UpdateConnectivity(DeviceConnectivityState state, DateTimeOffset timestamp)
    {
        LastSeenAt = timestamp;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Device name cannot be empty", nameof(newName));
        Name = newName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public record DeviceRegisteredEvent(DeviceId DeviceId, string Name, string ModelName) : DomainEvent;
public record DeviceAssignedEvent(DeviceId DeviceId, RoomId RoomId) : DomainEvent;