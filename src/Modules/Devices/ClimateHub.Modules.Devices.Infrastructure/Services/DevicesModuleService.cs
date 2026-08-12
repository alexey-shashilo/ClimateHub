using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.Devices.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Infrastructure.Services;

public class DevicesModuleService(IDeviceRepository deviceRepository) : IDevicesModule
{
    public async Task<bool> DeviceExistsAsync(DeviceId deviceId, CancellationToken cancellationToken = default)
    {
        return await deviceRepository.ExistsAsync(deviceId, cancellationToken);
    }

    public async Task<DeviceInfo?> GetDeviceAsync(DeviceId deviceId, CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        return device is null ? null : new DeviceInfo(device.Id, device.Name, device.Model.ModelName);
    }

    public async Task<RoomId?> GetDeviceRoomAssignmentAsync(DeviceId deviceId, CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        return device?.ActiveAssignment?.RoomId;
    }

    public async Task<bool> DeviceHasCapabilityAsync(DeviceId deviceId, string capabilityCode, CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        return device?.HasCapability(capabilityCode) ?? false;
    }

    public async Task<IReadOnlyCollection<DeviceBriefDto>> GetRoomDevicesAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var devices = await deviceRepository.GetByRoomAsync(roomId, cancellationToken);
        return devices.Select(d => new DeviceBriefDto(
            d.Id, d.HardwareId, d.Name,
            $"{d.Model.Manufacturer} {d.Model.ModelName}".Trim(),
            d.ConnectivityState.ToString().ToLowerInvariant(),
            d.LastSeenAt)).ToList();
    }
}
