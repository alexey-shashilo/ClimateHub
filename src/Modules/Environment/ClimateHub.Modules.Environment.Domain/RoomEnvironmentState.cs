using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Domain;

public class RoomEnvironmentState
{
    public RoomId RoomId { get; }
    public double? TemperatureC { get; private set; }
    public double? RelativeHumidityPct { get; private set; }
    public double? Co2Ppm { get; private set; }
    public DateTimeOffset? TemperatureMeasuredAt { get; private set; }
    public DateTimeOffset? HumidityMeasuredAt { get; private set; }
    public DateTimeOffset? Co2MeasuredAt { get; private set; }
    public DateTimeOffset? TemperatureReceivedAt { get; private set; }
    public DateTimeOffset? HumidityReceivedAt { get; private set; }
    public DateTimeOffset? Co2ReceivedAt { get; private set; }
    public DeviceId? TemperatureSourceDeviceId { get; private set; }
    public DeviceId? HumiditySourceDeviceId { get; private set; }
    public DeviceId? Co2SourceDeviceId { get; private set; }
    public MeasurementQuality TemperatureQuality { get; private set; }
    public MeasurementQuality HumidityQuality { get; private set; }
    public MeasurementQuality Co2Quality { get; private set; }

    public RoomEnvironmentState(RoomId roomId)
    {
        RoomId = roomId;
        TemperatureQuality = MeasurementQuality.Unknown;
        HumidityQuality = MeasurementQuality.Unknown;
        Co2Quality = MeasurementQuality.Unknown;
    }

    public MeasurementUpdateResult Update(
        DateTimeOffset measuredAt,
        double? temperatureC = null,
        double? relativeHumidityPct = null,
        double? co2Ppm = null,
        MeasurementQuality quality = MeasurementQuality.Valid,
        DeviceId? sourceDeviceId = null)
    {
        var result = MeasurementUpdateResult.Skipped;

        if (temperatureC.HasValue)
        {
            if (ShouldUpdate(measuredAt, TemperatureMeasuredAt, quality, TemperatureQuality))
            {
                TemperatureC = temperatureC.Value;
                TemperatureMeasuredAt = measuredAt;
                TemperatureReceivedAt = DateTimeOffset.UtcNow;
                TemperatureQuality = quality;
                TemperatureSourceDeviceId = sourceDeviceId;
                result = MeasurementUpdateResult.Updated;
            }
        }

        if (relativeHumidityPct.HasValue)
        {
            if (ShouldUpdate(measuredAt, HumidityMeasuredAt, quality, HumidityQuality))
            {
                RelativeHumidityPct = relativeHumidityPct.Value;
                HumidityMeasuredAt = measuredAt;
                HumidityReceivedAt = DateTimeOffset.UtcNow;
                HumidityQuality = quality;
                HumiditySourceDeviceId = sourceDeviceId;
                result = MeasurementUpdateResult.Updated;
            }
        }

        if (co2Ppm.HasValue)
        {
            if (ShouldUpdate(measuredAt, Co2MeasuredAt, quality, Co2Quality))
            {
                Co2Ppm = co2Ppm.Value;
                Co2MeasuredAt = measuredAt;
                Co2ReceivedAt = DateTimeOffset.UtcNow;
                Co2Quality = quality;
                Co2SourceDeviceId = sourceDeviceId;
                result = MeasurementUpdateResult.Updated;
            }
        }

        return result;
    }

    private static bool ShouldUpdate(
        DateTimeOffset candidateMeasuredAt,
        DateTimeOffset? currentMeasuredAt,
        MeasurementQuality candidateQuality,
        MeasurementQuality currentQuality)
    {
        if (currentMeasuredAt is null)
            return true;

        var cmp = candidateMeasuredAt.CompareTo(currentMeasuredAt.Value);
        if (cmp > 0) return true;
        if (cmp < 0) return false;

        return QualityScore(candidateQuality) > QualityScore(currentQuality);
    }

    private static int QualityScore(MeasurementQuality q) => q switch
    {
        MeasurementQuality.Valid => 100,
        MeasurementQuality.Estimated => 80,
        MeasurementQuality.Stale => 40,
        MeasurementQuality.Suspect => 20,
        MeasurementQuality.OutOfRange => 10,
        MeasurementQuality.SensorError => 5,
        _ => 0
    };
}

public enum MeasurementQuality
{
    Unknown,
    Valid,
    Estimated,
    Stale,
    Suspect,
    OutOfRange,
    SensorError
}

public enum MeasurementUpdateResult
{
    Skipped,
    Updated
}