using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Contracts;

public interface IDevicesModule
{
    Task<bool> DeviceExistsAsync(DeviceId deviceId, CancellationToken cancellationToken = default);
    Task<DeviceInfo?> GetDeviceAsync(DeviceId deviceId, CancellationToken cancellationToken = default);
    Task<RoomId?> GetDeviceRoomAssignmentAsync(DeviceId deviceId, CancellationToken cancellationToken = default);
    Task<bool> DeviceHasCapabilityAsync(DeviceId deviceId, string capabilityCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DeviceBriefDto>> GetRoomDevicesAsync(RoomId roomId, CancellationToken cancellationToken = default);
}

public record DeviceInfo(DeviceId Id, string Name, string ModelName);

public record DeviceBriefDto(
    DeviceId Id,
    string HardwareId,
    string Name,
    string Model,
    string Connectivity,
    DateTimeOffset? LastSeenAt);
