using ClimateHub.Modules.EngineeringSystems.Domain;

namespace ClimateHub.Modules.EngineeringSystems.Application.Resolvers;

public class EngineeringCapabilityFamilyResolver : IEngineeringCapabilityFamilyResolver
{
    private static readonly Dictionary<string, EngineeringCapabilityFamily> Mappings = new()
    {
        [EngineeringCapabilityCodes.ReduceCo2] = EngineeringCapabilityFamily.Ventilation,
        [EngineeringCapabilityCodes.IncreaseAirFlow] = EngineeringCapabilityFamily.Ventilation,
        [EngineeringCapabilityCodes.IncreaseTemperature] = EngineeringCapabilityFamily.Thermal,
        [EngineeringCapabilityCodes.DecreaseTemperature] = EngineeringCapabilityFamily.Thermal,
        [EngineeringCapabilityCodes.IncreaseHumidity] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.DecreaseHumidity] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.IncreaseHumidityPrecise] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.IncreaseHumiditySteam] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.IncreaseHumidityAdiabatic] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.HumidificationFlush] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.HumidificationDrain] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.HumidificationSterilize] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.HumidificationRecovery] = EngineeringCapabilityFamily.Humidification,
        [EngineeringCapabilityCodes.IncreaseIlluminance] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.DecreaseIlluminance] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.BlindPosition] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.LightingScene] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.LightingColor] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.LightingCct] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.OpenCurtains] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.CloseCurtains] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.CurtainPosition] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.ShadingProtection] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.DaylightHarvesting] = EngineeringCapabilityFamily.Lighting,
        [EngineeringCapabilityCodes.SunTracking] = EngineeringCapabilityFamily.Lighting,
    };

    public EngineeringCapabilityFamily ResolveFamily(string capabilityCode)
    {
        if (TryResolveFamily(capabilityCode, out var family))
            return family;
        return EngineeringCapabilityFamily.Standard;
    }

    public bool TryResolveFamily(string capabilityCode, out EngineeringCapabilityFamily family) =>
        Mappings.TryGetValue(capabilityCode, out family);

    public IReadOnlyDictionary<string, EngineeringCapabilityFamily> GetAllMappings() => Mappings;

    public void ValidateNoOverlaps()
    {
        var seen = new HashSet<string>();
        foreach (var key in Mappings.Keys)
        {
            if (!seen.Add(key))
                throw new InvalidOperationException($"Duplicate capability code mapping detected: {key}");
        }
    }
}
