namespace ClimateHub.Modules.EngineeringSystems.Domain;

public static class EngineeringCapabilityCodes
{
    // System-level capabilities (what engineering systems can do)
    public const string IncreaseTemperature = "eng.temperature.increase";
    public const string DecreaseTemperature = "eng.temperature.decrease";
    public const string IncreaseHumidity = "eng.humidity.increase";
    public const string DecreaseHumidity = "eng.humidity.decrease";
    public const string IncreaseHumidityPrecise = "eng.humidity.precise";
    public const string IncreaseHumiditySteam = "eng.humidity.steam";
    public const string IncreaseHumidityAdiabatic = "eng.humidity.adiabatic";
    public const string HumidificationFlush = "eng.humidity.flush";
    public const string HumidificationDrain = "eng.humidity.drain";
    public const string HumidificationSterilize = "eng.humidity.sterilize";
    public const string HumidificationRecovery = "eng.humidity.recovery";
    public const string IncreaseAirFlow = "eng.airflow.increase";
    public const string ReduceCo2 = "eng.co2.reduce";
    public const string IncreaseIlluminance = "eng.illuminance.increase";
    public const string DecreaseIlluminance = "eng.illuminance.decrease";
    public const string OpenCurtains = "eng.curtains.open";
    public const string CloseCurtains = "eng.curtains.close";
    public const string LightingScene = "eng.lighting.scene";
    public const string LightingColor = "eng.lighting.color";
    public const string LightingCct = "eng.lighting.cct";
    public const string BlindPosition = "eng.blind.position";
    public const string CurtainPosition = "eng.curtain.position";
    public const string ShadingProtection = "eng.shading.protection";
    public const string DaylightHarvesting = "eng.daylight.harvesting";
    public const string SunTracking = "eng.sun.tracking";

    // NeedType → Engineering Capability mapping
    private static readonly Dictionary<string, string> NeedToEngCapability = new()
    {
        ["TemperatureHeating"] = IncreaseTemperature,
        ["TemperatureCooling"] = DecreaseTemperature,
        ["HumidityIncrease"] = IncreaseHumidity,
        ["HumidityDecrease"] = DecreaseHumidity,
        ["Co2Reduction"] = ReduceCo2,
        ["IlluminanceIncrease"] = IncreaseIlluminance,
        ["IlluminanceDecrease"] = DecreaseIlluminance,
    };

    // Engineering Capability → Device Capability Codes (what device-level commands can satisfy it)
    private static readonly Dictionary<string, string[]> EngCapabilityToDeviceCodes = new()
    {
        [IncreaseTemperature] = ["control.damper-position"],
        [DecreaseTemperature] = ["control.fan-speed"],
        [IncreaseHumidity] = ["control.humidifier", "control.humidifier-output"],
        [IncreaseHumidityPrecise] = ["control.humidifier-output", "control.valve"],
        [IncreaseHumiditySteam] = ["control.steam-valve", "control.steam-generator"],
        [IncreaseHumidityAdiabatic] = ["control.nozzle-pump", "control.nozzle-valve"],
        [HumidificationFlush] = ["control.flush-valve"],
        [HumidificationDrain] = ["control.drain-valve"],
        [HumidificationSterilize] = ["control.uv-sterilizer"],
        [HumidificationRecovery] = ["control.valve"],
        [DecreaseHumidity] = ["control.fan-speed"],
        [ReduceCo2] = ["control.fan-speed"],
        [IncreaseAirFlow] = ["control.fan-speed", "control.damper-position"],
        [IncreaseIlluminance] = ["control.lighting", "control.dimmer"],
        [DecreaseIlluminance] = ["control.lighting", "control.dimmer"],
        [LightingScene] = ["control.scene"],
        [LightingColor] = ["control.rgb"],
        [LightingCct] = ["control.cct"],
        [BlindPosition] = ["control.blind-motor"],
        [CurtainPosition] = ["control.curtain-motor"],
        [ShadingProtection] = ["control.blind-motor", "control.facade-screen"],
        [OpenCurtains] = ["control.relay", "control.curtain-motor"],
        [CloseCurtains] = ["control.relay", "control.curtain-motor"],
    };

    private static readonly Dictionary<string, string> EngCapabilityToDeviceRole = new()
    {
        [ReduceCo2] = DeviceRole.SupplyFan,
        [IncreaseAirFlow] = DeviceRole.SupplyDamper,
    };

    private static readonly Dictionary<string, string[]> HvacCapabilityToDeviceRoles = new()
    {
        [IncreaseHumidity] = [DeviceRole.SteamGenerator, DeviceRole.SteamValve, DeviceRole.SteamInjector, DeviceRole.HumiditySensorReference],
        [IncreaseHumiditySteam] = [DeviceRole.SteamGenerator, DeviceRole.SteamValve, DeviceRole.SteamInjector],
        [IncreaseHumidityAdiabatic] = [DeviceRole.NozzlePump, DeviceRole.NozzleValve, DeviceRole.HumidifierFan],
        [ReduceCo2] = [DeviceRole.SupplyFan, DeviceRole.ExhaustFan, DeviceRole.SupplyDamper],
        [IncreaseAirFlow] = [DeviceRole.SupplyFan, DeviceRole.ExhaustFan, DeviceRole.SupplyDamper],
    };

    public static string? GetEngCapability(string needType) =>
        NeedToEngCapability.GetValueOrDefault(needType);

    public static string[] GetDeviceCodes(string engCapability) =>
        EngCapabilityToDeviceCodes.GetValueOrDefault(engCapability, Array.Empty<string>());

    public static string? GetDeviceRole(string engCapabilityCode) =>
        EngCapabilityToDeviceRole.GetValueOrDefault(engCapabilityCode);

    public static string[] GetRequiredDeviceRoles(string engCapability) =>
        HvacCapabilityToDeviceRoles.GetValueOrDefault(engCapability, Array.Empty<string>());

    // Default system type for each engineering capability
    private static readonly Dictionary<string, SystemType> EngCapabilityToSystem = new()
    {
        [IncreaseTemperature] = SystemType.Heating,
        [DecreaseTemperature] = SystemType.Cooling,
        [IncreaseHumidity] = SystemType.Humidification,
        [DecreaseHumidity] = SystemType.Dehumidification,
        [ReduceCo2] = SystemType.SupplyVentilation,
        [IncreaseAirFlow] = SystemType.SupplyVentilation,
        [IncreaseIlluminance] = SystemType.Lighting,
        [DecreaseIlluminance] = SystemType.Lighting,
        [OpenCurtains] = SystemType.Curtains,
        [CloseCurtains] = SystemType.Curtains,
    };

    public static SystemType? GetSystemType(string engCapability) =>
        EngCapabilityToSystem.GetValueOrDefault(engCapability);
}
