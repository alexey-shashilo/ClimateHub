namespace ClimateHub.Modules.Devices.Application.OpenApi;

public record DeviceSummaryDto(
    string Id,
    string HardwareId,
    string Name,
    string Model,
    string DeviceType,
    string Status,
    string Connectivity,
    string? FirmwareVersion,
    DateTimeOffset? LastSeenAt,
    string? AssignedRoomId,
    string? AssignedRoomName,
    IReadOnlyCollection<DeviceCapabilityDto> Capabilities);

public record DeviceCapabilityDto(
    string Code,
    string Kind,
    string? Unit,
    double? Minimum,
    double? Maximum,
    bool Writable,
    bool Available);
