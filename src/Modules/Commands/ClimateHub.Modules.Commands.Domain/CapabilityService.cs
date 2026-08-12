namespace ClimateHub.Modules.Commands.Domain;

public class CapabilityService
{
    public Task<CapabilityCommandDefinition?> GetDefinitionAsync(string capabilityCode, CancellationToken ct = default)
    {
        return Task.FromResult(CapabilityRegistry.Get(capabilityCode));
    }
}
