using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.Needs.Domain;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class CapabilityPlanner
{
    // Canonical mapping lives in EngineeringCapabilityCodes.EngCapabilityToDeviceCodes.
    // This dictionary maps Needs to the first device capability code from that canonical source.
    private static readonly Dictionary<NeedType, string> NeedToDeviceCapability = new()
    {
        [NeedType.TemperatureHeating] = "control.damper-position",
        [NeedType.TemperatureCooling] = "control.fan-speed",
        [NeedType.HumidityIncrease] = "control.humidifier",
        [NeedType.HumidityDecrease] = "control.fan-speed",
        [NeedType.Co2Reduction] = "control.fan-speed",
        [NeedType.IlluminanceIncrease] = "control.lighting",
        [NeedType.IlluminanceDecrease] = "control.lighting",
    };

    private static readonly Dictionary<NeedType, string> NeedToEngCapability = new()
    {
        [NeedType.TemperatureHeating] = EngineeringCapabilityCodes.IncreaseTemperature,
        [NeedType.TemperatureCooling] = EngineeringCapabilityCodes.DecreaseTemperature,
        [NeedType.HumidityIncrease] = EngineeringCapabilityCodes.IncreaseHumidity,
        [NeedType.HumidityDecrease] = EngineeringCapabilityCodes.DecreaseHumidity,
        [NeedType.Co2Reduction] = EngineeringCapabilityCodes.ReduceCo2,
        [NeedType.IlluminanceIncrease] = EngineeringCapabilityCodes.IncreaseIlluminance,
        [NeedType.IlluminanceDecrease] = EngineeringCapabilityCodes.DecreaseIlluminance,
    };

    public static string? GetCapabilityCode(NeedType needType) =>
        NeedToDeviceCapability.GetValueOrDefault(needType);

    public static string? GetEngineeringCapabilityCode(NeedType needType) =>
        NeedToEngCapability.GetValueOrDefault(needType);

    public static NeedType? GetNeedType(string capabilityCode) =>
        NeedToDeviceCapability.FirstOrDefault(kv => kv.Value == capabilityCode).Key;
}
