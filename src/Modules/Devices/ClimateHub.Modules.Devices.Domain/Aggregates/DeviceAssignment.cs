using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Domain.Aggregates;

public enum AssignmentStatus
{
    Active,
    Completed
}

public class DeviceAssignment : Entity<long>
{
    public DeviceId DeviceId { get; private set; }
    public RoomId RoomId { get; private set; }
    public DateTimeOffset AssignedAt { get; private init; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public AssignmentStatus Status { get; private set; }

    private DeviceAssignment()
    {
    } // EF Core

    private DeviceAssignment(long id, DeviceId deviceId, RoomId roomId)
    {
        Id = id;
        DeviceId = deviceId;
        RoomId = roomId;
        AssignedAt = DateTimeOffset.UtcNow;
        Status = AssignmentStatus.Active;
    }

    public static DeviceAssignment Create(DeviceId deviceId, RoomId roomId)
    {
        return new DeviceAssignment(0, deviceId, roomId);
    }

    public void Complete()
    {
        Status = AssignmentStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
