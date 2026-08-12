using ClimateHub.Modules.Devices.Domain.Aggregates;

namespace ClimateHub.Modules.Devices.Application.Commands;

public record DeviceDto(
    string Id,
    string HardwareId,
    string Name,
    string Manufacturer,
    string ModelName,
    string? HardwareVersion,
    string ProtocolVersion,
    string Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastSeenAt,
    string? CurrentBootId,
    long LastSequenceNumber,
    string? AssignedRoomId,
    IReadOnlyCollection<DeviceCapabilityDto> Capabilities)
{
    public static DeviceDto From(Domain.Aggregates.Device device) => new(
        device.Id.ToString(),
        device.HardwareId,
        device.Name,
        device.Model.Manufacturer,
        device.Model.ModelName,
        device.Model.HardwareVersion,
        device.ProtocolVersion,
        device.Status.ToString(),
        device.RegisteredAt,
        device.LastSeenAt,
        device.CurrentBootId?.ToString(),
        device.LastSequenceNumber,
        device.ActiveAssignment?.RoomId.ToString(),
        device.Capabilities.Select(DeviceCapabilityDto.From).ToList());
}

public record DeviceCapabilityDto(
    string Code,
    string DataType,
    string? Unit,
    double? MinValue,
    double? MaxValue,
    string Status)
{
    public static DeviceCapabilityDto From(DeviceCapability capability) => new(
        capability.Code,
        capability.DataType,
        capability.Unit,
        capability.MinValue,
        capability.MaxValue,
        capability.Status);
}
