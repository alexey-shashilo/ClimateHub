using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Devices.Contracts;

public interface ICapabilityCatalog
{
    Task<CapabilityDefinitionDto?> GetDefinitionAsync(string capabilityCode, CancellationToken ct = default);
    Task<IReadOnlyCollection<CapabilityDefinitionDto>> GetDeviceCapabilitiesAsync(DeviceId deviceId, CancellationToken ct = default);
    Task<bool> HasCapabilityAsync(DeviceId deviceId, string capabilityCode, CancellationToken ct = default);
}

public record CapabilityDefinitionDto(
    string Code,
    string Kind,
    string? DataType,
    string? Unit,
    bool Writable,
    IReadOnlyCollection<string> SupportedOperations,
    double? Minimum,
    double? Maximum,
    int ContractVersion,
    bool SupportsProgress,
    bool SupportsCancellation);