using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Infrastructure.Services;

public interface IInfluxDbWriter
{
    Task WriteMeasurementAsync(
        RoomId roomId,
        DeviceId deviceId,
        DateTimeOffset measuredAt,
        double? temperatureC,
        double? relativeHumidityPct,
        double? co2Ppm,
        string quality,
        CancellationToken cancellationToken = default);
}

public interface IInfluxDbReader
{
    Task<IReadOnlyCollection<InfluxMeasurement>> ReadHistoryAsync(
        RoomId roomId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

public record InfluxMeasurement(
    DateTimeOffset MeasuredAt,
    double? TemperatureC,
    double? RelativeHumidityPct,
    double? Co2Ppm,
    string Quality);