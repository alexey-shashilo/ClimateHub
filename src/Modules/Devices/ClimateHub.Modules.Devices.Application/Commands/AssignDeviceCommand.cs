using ClimateHub.Modules.Building.Contracts;
using ClimateHub.Modules.Devices.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Application.Commands;

public record AssignDeviceCommand
{
    public DeviceId DeviceId { get; init; }
    public RoomId RoomId { get; init; }
}

public class AssignDeviceHandler(
    IDeviceRepository deviceRepository,
    IBuildingModule buildingModule)
{
    public async Task<DeviceDto> HandleAsync(AssignDeviceCommand command, CancellationToken ct = default)
    {
        var device = await deviceRepository.GetByIdAsync(command.DeviceId, ct);
        if (device is null)
            throw new KeyNotFoundException("DEVICE_NOT_FOUND");

        var roomExists = await buildingModule.RoomExistsAsync(command.RoomId, ct);
        if (!roomExists)
            throw new KeyNotFoundException("ROOM_NOT_FOUND");

        device.AssignToRoom(command.RoomId);
        await deviceRepository.UpdateAndSaveAsync(device, ct);
        var updated = await deviceRepository.GetByIdAsync(command.DeviceId, ct);
        return DeviceDto.From(updated ?? device);
    }
}