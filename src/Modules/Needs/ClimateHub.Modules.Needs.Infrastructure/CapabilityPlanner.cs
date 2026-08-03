using ClimateHub.Modules.Needs.Domain;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class CapabilityPlanner
{
    private static readonly Dictionary<NeedType, string> NeedToCapability = new()
    {
        [NeedType.TemperatureHeating] = "control.damper-position",
        [NeedType.TemperatureCooling] = "control.fan-speed",
        [NeedType.HumidityIncrease] = "control.humidifier",
        [NeedType.HumidityDecrease] = "control.fan-speed",
        [NeedType.Co2Reduction] = "control.fan-speed",
        [NeedType.IlluminanceIncrease] = "control.lighting",
        [NeedType.IlluminanceDecrease] = "control.lighting",
    };

    // NeedType → Engineering Capability mapping (high-level, for Engineering Systems routing)
    private static readonly Dictionary<NeedType, string> NeedToEngCapability = new()
    {
        [NeedType.TemperatureHeating] = "eng.temperature.increase",
        [NeedType.TemperatureCooling] = "eng.temperature.decrease",
        [NeedType.HumidityIncrease] = "eng.humidity.increase",
        [NeedType.HumidityDecrease] = "eng.humidity.decrease",
        [NeedType.Co2Reduction] = "eng.co2.reduce",
        [NeedType.IlluminanceIncrease] = "eng.illuminance.increase",
        [NeedType.IlluminanceDecrease] = "eng.illuminance.decrease",
    };

    public static string? GetCapabilityCode(NeedType needType) =>
        NeedToCapability.GetValueOrDefault(needType);

    public static string? GetEngineeringCapabilityCode(NeedType needType) =>
        NeedToEngCapability.GetValueOrDefault(needType);

    public static NeedType? GetNeedType(string capabilityCode) =>
        NeedToCapability.FirstOrDefault(kv => kv.Value == capabilityCode).Key;
}