using System.Text.Json;

namespace ClimateHub.DeviceGateway.Services;

/// <summary>
/// Central JSON settings for device gateway telemetry payloads.
/// MQTT device payloads use camelCase (e.g. <c>messageId</c>) while domain contracts
/// use PascalCase properties; case-insensitive binding is required.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
