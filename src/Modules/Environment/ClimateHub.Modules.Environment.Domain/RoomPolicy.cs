using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Environment.Domain;

public enum PolicyControlMode { MonitorOnly, Manual, Automatic, Disabled }

public class RoomPolicy
{
    public RoomId Id { get; private set; }
    public double? TemperatureMin { get; private set; }
    public double? TemperatureMax { get; private set; }
    public double? TemperaturePreferred { get; private set; }
    public PolicyControlMode TemperatureMode { get; private set; } = PolicyControlMode.MonitorOnly;

    public double? HumidityMin { get; private set; }
    public double? HumidityMax { get; private set; }
    public double? HumidityPreferred { get; private set; }
    public PolicyControlMode HumidityMode { get; private set; } = PolicyControlMode.MonitorOnly;

    public double? Co2Min { get; private set; }
    public double? Co2Max { get; private set; }
    public double? Co2Preferred { get; private set; }
    public PolicyControlMode Co2Mode { get; private set; } = PolicyControlMode.MonitorOnly;

    public double? IlluminanceMin { get; private set; }
    public double? IlluminanceMax { get; private set; }
    public double? IlluminancePreferred { get; private set; }
    public PolicyControlMode IlluminanceMode { get; private set; } = PolicyControlMode.MonitorOnly;

    public DateTimeOffset UpdatedAt { get; private set; }

    private RoomPolicy() { }

    public static RoomPolicy Create(RoomId roomId,
        double? tMin = null, double? tMax = null, double? tPref = null, PolicyControlMode? tMode = null,
        double? hMin = null, double? hMax = null, double? hPref = null, PolicyControlMode? hMode = null,
        double? cMin = null, double? cMax = null, double? cPref = null, PolicyControlMode? cMode = null,
        double? iMin = null, double? iMax = null, double? iPref = null, PolicyControlMode? iMode = null)
    {
        return new RoomPolicy
        {
            Id = roomId,
            TemperatureMin = tMin, TemperatureMax = tMax, TemperaturePreferred = tPref, TemperatureMode = tMode ?? PolicyControlMode.MonitorOnly,
            HumidityMin = hMin, HumidityMax = hMax, HumidityPreferred = hPref, HumidityMode = hMode ?? PolicyControlMode.MonitorOnly,
            Co2Min = cMin, Co2Max = cMax, Co2Preferred = cPref, Co2Mode = cMode ?? PolicyControlMode.MonitorOnly,
            IlluminanceMin = iMin, IlluminanceMax = iMax, IlluminancePreferred = iPref, IlluminanceMode = iMode ?? PolicyControlMode.MonitorOnly,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(double? tMin = null, double? tMax = null, double? tPref = null, PolicyControlMode? tMode = null,
        double? hMin = null, double? hMax = null, double? hPref = null, PolicyControlMode? hMode = null,
        double? cMin = null, double? cMax = null, double? cPref = null, PolicyControlMode? cMode = null,
        double? iMin = null, double? iMax = null, double? iPref = null, PolicyControlMode? iMode = null)
    {
        if (tMin.HasValue) TemperatureMin = tMin;
        if (tMax.HasValue) TemperatureMax = tMax;
        if (tPref.HasValue) TemperaturePreferred = tPref;
        if (tMode.HasValue) TemperatureMode = tMode.Value;

        if (hMin.HasValue) HumidityMin = hMin;
        if (hMax.HasValue) HumidityMax = hMax;
        if (hPref.HasValue) HumidityPreferred = hPref;
        if (hMode.HasValue) HumidityMode = hMode.Value;

        if (cMin.HasValue) Co2Min = cMin;
        if (cMax.HasValue) Co2Max = cMax;
        if (cPref.HasValue) Co2Preferred = cPref;
        if (cMode.HasValue) Co2Mode = cMode.Value;

        if (iMin.HasValue) IlluminanceMin = iMin;
        if (iMax.HasValue) IlluminanceMax = iMax;
        if (iPref.HasValue) IlluminancePreferred = iPref;
        if (iMode.HasValue) IlluminanceMode = iMode.Value;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public PolicyControlMode GetControlMode(string parameter) => parameter switch
    {
        "temperature" => TemperatureMode,
        "humidity" => HumidityMode,
        "co2" => Co2Mode,
        "illuminance" => IlluminanceMode,
        _ => PolicyControlMode.Disabled
    };
}

public record RoomPolicyDto(
    double? TemperatureMin, double? TemperatureMax, double? TemperaturePreferred, string? TemperatureMode,
    double? HumidityMin, double? HumidityMax, double? HumidityPreferred, string? HumidityMode,
    double? Co2Min, double? Co2Max, double? Co2Preferred, string? Co2Mode,
    DateTimeOffset UpdatedAt);