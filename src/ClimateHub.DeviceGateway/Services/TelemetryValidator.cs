using ClimateHub.DeviceGateway.Contracts;

namespace ClimateHub.DeviceGateway.Services;

public static class TelemetryValidator
{
    public static TelemetryParseResult Validate(EnvironmentTelemetryEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.MessageId))
            return Fail("missing_message_id", "messageId is required");

        if (string.IsNullOrWhiteSpace(envelope.ProtocolVersion))
            return Fail("missing_protocol_version", "protocolVersion is required");

        if (!envelope.ProtocolVersion.StartsWith("1."))
            return Fail("unsupported_protocol_version",
                $"protocolVersion '{envelope.ProtocolVersion}' is not supported. Expected 1.x");

        if (string.IsNullOrWhiteSpace(envelope.BuildingId))
            return Fail("missing_building_id", "buildingId is required");

        if (string.IsNullOrWhiteSpace(envelope.DeviceId))
            return Fail("missing_device_id", "deviceId is required");

        if (envelope.SequenceNumber < 0)
            return Fail("invalid_sequence_number", "sequenceNumber must be non-negative");

        if (!Guid.TryParse(envelope.MessageId, out _))
            return Fail("invalid_message_id", "messageId must be a valid UUID");

        if (envelope.Payload is null)
            return Fail("missing_payload", "payload is required");

        return new TelemetryParseResult { Success = true };
    }

    public static TelemetryParseResult ValidatePayload(EnvironmentTelemetryPayload payload)
    {
        if (payload.TemperatureC.HasValue && (payload.TemperatureC < -50 || payload.TemperatureC > 70))
            return Fail("invalid_temperature", "TemperatureC must be between -50 and 70");

        if (payload.RelativeHumidityPct.HasValue && (payload.RelativeHumidityPct < 0 || payload.RelativeHumidityPct > 100))
            return Fail("invalid_humidity", "RelativeHumidityPct must be between 0 and 100");

        if (payload.Co2Ppm.HasValue && (payload.Co2Ppm < 0 || payload.Co2Ppm > 10000))
            return Fail("invalid_co2", "Co2Ppm must be between 0 and 10000");

        if (!payload.TemperatureC.HasValue && !payload.RelativeHumidityPct.HasValue && !payload.Co2Ppm.HasValue)
            return Fail("empty_payload", "payload must contain at least one measurement");

        return new TelemetryParseResult { Success = true };
    }

    private static TelemetryParseResult Fail(string code, string message)
    {
        return new TelemetryParseResult
        {
            Success = false,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}