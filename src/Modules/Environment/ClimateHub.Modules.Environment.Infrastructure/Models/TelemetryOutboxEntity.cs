using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Models;

public class TelemetryOutboxEntity
{
    public long Id { get; set; }
    public RoomId RoomId { get; set; }
    public DeviceId DeviceId { get; set; }
    public DateTimeOffset MeasuredAt { get; set; }
    public double? TemperatureC { get; set; }
    public double? RelativeHumidityPct { get; set; }
    public double? Co2Ppm { get; set; }
    public string Quality { get; set; } = "valid";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AvailableAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorCode { get; set; }
}
