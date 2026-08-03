using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Domain;

public static class RoomEnvironmentStateExtensions
{
    public static EnvironmentSnapshot ToSnapshot(this RoomEnvironmentState state) => new(
        state.RoomId,
        state.TemperatureC,
        state.TemperatureMeasuredAt,
        state.TemperatureReceivedAt,
        state.TemperatureQuality,
        state.TemperatureSourceDeviceId,
        state.RelativeHumidityPct,
        state.HumidityMeasuredAt,
        state.HumidityReceivedAt,
        state.HumidityQuality,
        state.HumiditySourceDeviceId,
        state.Co2Ppm,
        state.Co2MeasuredAt,
        state.Co2ReceivedAt,
        state.Co2Quality,
        state.Co2SourceDeviceId);
}

public record ParameterSnapshot(
    double? Value,
    DateTimeOffset? MeasuredAt,
    DateTimeOffset? ReceivedAt,
    MeasurementQuality Quality,
    DeviceId? SourceDeviceId);

public record EnvironmentSnapshot(
    RoomId RoomId,
    double? TemperatureC,
    DateTimeOffset? TemperatureMeasuredAt,
    DateTimeOffset? TemperatureReceivedAt,
    MeasurementQuality TemperatureQuality,
    DeviceId? TemperatureSourceDeviceId,
    double? HumidityPct,
    DateTimeOffset? HumidityMeasuredAt,
    DateTimeOffset? HumidityReceivedAt,
    MeasurementQuality HumidityQuality,
    DeviceId? HumiditySourceDeviceId,
    double? Co2Ppm,
    DateTimeOffset? Co2MeasuredAt,
    DateTimeOffset? Co2ReceivedAt,
    MeasurementQuality Co2Quality,
    DeviceId? Co2SourceDeviceId)
{
    public ParameterSnapshot? Temperature => TemperatureC.HasValue
        ? new(TemperatureC, TemperatureMeasuredAt, TemperatureReceivedAt, TemperatureQuality, TemperatureSourceDeviceId)
        : null;

    public ParameterSnapshot? Humidity => HumidityPct.HasValue
        ? new(HumidityPct, HumidityMeasuredAt, HumidityReceivedAt, HumidityQuality, HumiditySourceDeviceId)
        : null;

    public ParameterSnapshot? Co2 => Co2Ppm.HasValue
        ? new(Co2Ppm, Co2MeasuredAt, Co2ReceivedAt, Co2Quality, Co2SourceDeviceId)
        : null;
}