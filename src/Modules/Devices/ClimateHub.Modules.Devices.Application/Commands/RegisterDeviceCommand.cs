using ClimateHub.Modules.Devices.Domain.Aggregates;
using ClimateHub.Modules.Devices.Domain.Repositories;

namespace ClimateHub.Modules.Devices.Application.Commands;

public record RegisterDeviceCommand
{
    public string HardwareId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public string? HardwareVersion { get; init; }
    public string ProtocolVersion { get; init; } = "1.0";
}

public class RegisterDeviceHandler(IDeviceRepository deviceRepository)
{
    public async Task<DeviceDto> HandleAsync(RegisterDeviceCommand command, CancellationToken ct = default)
    {
        var existing = await deviceRepository.GetByHardwareIdAsync(command.HardwareId, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Device with HardwareId '{command.HardwareId}' is already registered");

        var model = new DeviceModel(command.Manufacturer, command.ModelName, command.HardwareVersion);
        var device = Domain.Aggregates.Device.Register(command.HardwareId, command.Name, model, command.ProtocolVersion);

        device.AddCapability(DeviceCapability.Temperature(device.Id));
        device.AddCapability(DeviceCapability.Humidity(device.Id));
        device.AddCapability(DeviceCapability.Co2(device.Id));

        await deviceRepository.AddAsync(device, ct);

        return DeviceDto.From(device);
    }
}