using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Repositories;

public class RoomParameterEntity
{
    public long Id { get; set; }
    public RoomId RoomId { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public double? Value { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset? MeasuredAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string Quality { get; set; } = "Unknown";
    public DeviceId? SourceDeviceId { get; set; }
}