using ClimateHub.Modules.Devices.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Application.Commands;

public record UpdateDeviceCommand { public DeviceId Id { get; init; } public string Name { get; init; } = ""; }
public class UpdateDeviceHandler(IDeviceRepository repo)
{
    public async Task HandleAsync(UpdateDeviceCommand cmd, CancellationToken ct = default)
    {
        var d = await repo.GetByIdAsync(cmd.Id, ct);
        if (d is null) throw new KeyNotFoundException("DEVICE_NOT_FOUND");
        d.Rename(cmd.Name);
        await repo.UpdateAndSaveAsync(d, ct);
    }
}

public record DeleteDeviceCommand { public DeviceId Id { get; init; } }
public class DeleteDeviceHandler(IDeviceRepository repo)
{
    public async Task HandleAsync(DeleteDeviceCommand cmd, CancellationToken ct = default)
    {
        var d = await repo.GetByIdAsync(cmd.Id, ct);
        if (d is null) throw new KeyNotFoundException("DEVICE_NOT_FOUND");
        await repo.RemoveAndSaveAsync(d, ct);
    }
}
