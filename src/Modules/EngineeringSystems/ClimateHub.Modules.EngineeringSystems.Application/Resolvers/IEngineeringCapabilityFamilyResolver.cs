namespace ClimateHub.Modules.EngineeringSystems.Application.Resolvers;

public enum EngineeringCapabilityFamily
{
    Ventilation,
    Thermal,
    Humidification,
    Lighting,
    Standard
}

public interface IEngineeringCapabilityFamilyResolver
{
    EngineeringCapabilityFamily ResolveFamily(string capabilityCode);
    bool TryResolveFamily(string capabilityCode, out EngineeringCapabilityFamily family);
    IReadOnlyDictionary<string, EngineeringCapabilityFamily> GetAllMappings();
    void ValidateNoOverlaps();
}
