using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Infrastructure;

public class CapabilityCatalogService : ICapabilityCatalog
{
    private static readonly List<CapabilityDefinitionDto> Definitions =
    [
        new("control.relay", "control", "boolean", null, true,
            ["set"], null, null, 1, false, true),
        new("control.fan-speed", "control", "number", "percent", true,
            ["set"], 0.0, 100.0, 1, true, false),
        new("control.damper-position", "control", "number", "percent", true,
            ["set"], 0.0, 100.0, 1, true, true),
        new("control.humidifier", "control", "number", "percent", true,
            ["set"], 0.0, 100.0, 1, true, true),
        new("control.lighting", "control", "number", "percent", true,
            ["set"], 0.0, 100.0, 1, true, true),
        new("control.dimmer", "control", "number", "percent", true,
            ["set"], 0.0, 100.0, 1, true, false),
        new("measure.temperature", "measurement", "number", "celsius", false,
            ["read"], -50.0, 70.0, 1, false, false),
        new("measure.relative-humidity", "measurement", "number", "percent", false,
            ["read"], 0.0, 100.0, 1, false, false),
        new("measure.co2", "measurement", "number", "ppm", false,
            ["read"], 0.0, 10000.0, 1, false, false),
        new("measure.illuminance", "measurement", "number", "lux", false,
            ["read"], 0.0, 200000.0, 1, false, false),
    ];

    public Task<CapabilityDefinitionDto?> GetDefinitionAsync(string capabilityCode, CancellationToken ct = default)
    {
        return Task.FromResult(Definitions.FirstOrDefault(d => d.Code == capabilityCode));
    }

    public Task<IReadOnlyCollection<CapabilityDefinitionDto>> GetDeviceCapabilitiesAsync(DeviceId deviceId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyCollection<CapabilityDefinitionDto>>(Definitions);
    }

    public Task<bool> HasCapabilityAsync(DeviceId deviceId, string capabilityCode, CancellationToken ct = default)
    {
        return Task.FromResult(Definitions.Any(d => d.Code == capabilityCode));
    }
}