using System.Data;
using System.Text.Json;

namespace ClimateHub.Modules.Commands.Domain;

public class CapabilityCommandDefinition
{
    public string CapabilityCode { get; init; } = "";
    public string[] SupportedOperations { get; init; } = [];
    public string ParameterSchemaJson { get; init; } = "{}";
    public bool SupportsProgress { get; init; }
    public bool SupportsCancellation { get; init; }
    public TimeSpan DefaultTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan MaximumTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public int ContractVersion { get; init; } = 1;

    public bool ValidateParameters(string operation, string parametersJson, out string? error)
    {
        error = null;
        if (!SupportedOperations.Contains(operation))
        { error = $"Operation '{operation}' not supported for {CapabilityCode}"; return false; }
        try
        {
            var doc = JsonDocument.Parse(parametersJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            { error = "Parameters must be a JSON object"; return false; }
            return true;
        }
        catch (JsonException) { error = "Invalid JSON parameters"; return false; }
    }
}

public static class CapabilityRegistry
{
    private static readonly Dictionary<string, CapabilityCommandDefinition> _defs = new()
    {
        ["control.relay"] = new CapabilityCommandDefinition
        {
            CapabilityCode = "control.relay",
            SupportedOperations = ["set"],
            ParameterSchemaJson = """{"type":"object","properties":{"enabled":{"type":"boolean"}},"required":["enabled"]}""",
            SupportsCancellation = true,
            DefaultTimeout = TimeSpan.FromSeconds(15),
        },
        ["control.fan-speed"] = new CapabilityCommandDefinition
        {
            CapabilityCode = "control.fan-speed",
            SupportedOperations = ["set"],
            ParameterSchemaJson = """{"type":"object","properties":{"speedPct":{"type":"number","minimum":0,"maximum":100}},"required":["speedPct"]}""",
            SupportsProgress = true,
            DefaultTimeout = TimeSpan.FromSeconds(30),
        },
        ["control.damper-position"] = new CapabilityCommandDefinition
        {
            CapabilityCode = "control.damper-position",
            SupportedOperations = ["set"],
            ParameterSchemaJson = """{"type":"object","properties":{"positionPct":{"type":"number","minimum":0,"maximum":100}},"required":["positionPct"]}""",
            SupportsProgress = true,
            SupportsCancellation = true,
            DefaultTimeout = TimeSpan.FromSeconds(30),
        },
    };

    public static CapabilityCommandDefinition? Get(string capabilityCode) =>
        _defs.GetValueOrDefault(capabilityCode);

    public static bool Exists(string capabilityCode) => _defs.ContainsKey(capabilityCode);
}