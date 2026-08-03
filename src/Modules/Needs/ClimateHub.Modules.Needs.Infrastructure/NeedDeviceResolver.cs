using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.Infrastructure;

public enum DeviceResolutionResult
{
    Resolved,
    NotFound,
    Ambiguous,
    Offline,
    Unavailable,
    CapabilityNotWritable,
    CapabilityOutOfRange,
    ProtocolIncompatible
}

public record DeviceResolution(DeviceResolutionResult Result, DeviceId? DeviceId = null, string? CapabilityCode = null);

public class NeedDeviceResolver
{
    private readonly IDevicesModule _devicesModule;
    private readonly ICapabilityCatalog _capabilityCatalog;

    public NeedDeviceResolver(IDevicesModule devicesModule, ICapabilityCatalog capabilityCatalog)
    {
        _devicesModule = devicesModule;
        _capabilityCatalog = capabilityCatalog;
    }

    public async Task<DeviceResolution> ResolveAsync(RoomId roomId, string capabilityCode, DeviceId? preferredDeviceId = null, CancellationToken ct = default)
    {
        var devices = await _devicesModule.GetRoomDevicesAsync(roomId, ct);
        if (devices.Count == 0)
            return new DeviceResolution(DeviceResolutionResult.NotFound);

        var def = await _capabilityCatalog.GetDefinitionAsync(capabilityCode, ct);
        if (def is null)
            return new DeviceResolution(DeviceResolutionResult.NotFound);

        var compatible = new List<(DeviceId id, string name)>();

        foreach (var d in devices)
        {
            if (d.Connectivity != "online" && d.Connectivity != "operational")
                continue;

            var hasCap = await _devicesModule.DeviceHasCapabilityAsync(d.Id, capabilityCode, ct);
            if (!hasCap)
                continue;

            compatible.Add((d.Id, d.Name));
        }

        if (compatible.Count == 0)
        {
            var anyOfflineCapable = false;
            foreach (var d in devices)
            {
                if (await _devicesModule.DeviceHasCapabilityAsync(d.Id, capabilityCode, ct))
                { anyOfflineCapable = true; break; }
            }
            return new DeviceResolution(anyOfflineCapable ? DeviceResolutionResult.Offline : DeviceResolutionResult.NotFound);
        }

        if (preferredDeviceId.HasValue)
        {
            var preferred = compatible.FirstOrDefault(c => c.id == preferredDeviceId.Value);
            if (preferred != default)
                return new DeviceResolution(DeviceResolutionResult.Resolved, preferred.id, capabilityCode);
        }

        if (compatible.Count > 1)
            return new DeviceResolution(DeviceResolutionResult.Ambiguous);

        return new DeviceResolution(DeviceResolutionResult.Resolved, compatible[0].id, capabilityCode);
    }
}