using System.Text.Json;

namespace ClimateHub.DeviceGateway.Contracts;

public class EnvironmentTelemetryEnvelope
{
    public string MessageId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string ProtocolVersion { get; set; } = string.Empty;
    public string BuildingId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string? BootId { get; set; }
    public long SequenceNumber { get; set; }
    public string? MeasuredAt { get; set; }
    public JsonElement? Payload { get; set; }
}

public class EnvironmentTelemetryPayload
{
    public double? TemperatureC { get; set; }
    public double? RelativeHumidityPct { get; set; }
    public double? Co2Ppm { get; set; }
}

public class TelemetryParseResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class TelemetryTopicParser
{
    public const string TopicPattern = "climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment";

    public static bool TryParseTopic(string topic, out string buildingId, out string deviceId)
    {
        buildingId = string.Empty;
        deviceId = string.Empty;

        var parts = topic.Split('/');
        if (parts.Length < 6) return false;
        if (parts[0] != "climate-hub") return false;
        if (parts[1] != "v1") return false;

        buildingId = parts[2];
        deviceId = parts[3];

        var suffix = string.Join("/", parts[4..]);
        return suffix == "telemetry/environment";
    }
}