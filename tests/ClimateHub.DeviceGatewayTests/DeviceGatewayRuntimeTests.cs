using System.Text;
using System.Text.Json;
using ClimateHub.DeviceGateway.Services;
using ClimateHub.Modules.Commands.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.DeviceGatewayTests;

public class DeviceGatewayRuntimeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void TelemetryValidator_ValidEnvelope_ReturnsSuccess()
    {
        var envelope = CreateValidEnvelope();
        var result = TelemetryValidator.Validate(envelope);
        Assert.True(result.Success);
    }

    [Fact]
    public void TelemetryValidator_MissingMessageId_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { MessageId = "" };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("missing_message_id", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_InvalidProtocolVersion_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { ProtocolVersion = "2.0" };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("unsupported_protocol_version", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_InvalidMessageId_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { MessageId = "not-a-guid" };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("invalid_message_id", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_NegativeSequenceNumber_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { SequenceNumber = -1 };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("invalid_sequence_number", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_MissingBuildingId_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { BuildingId = "" };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("missing_building_id", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_MissingDeviceId_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { DeviceId = "" };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("missing_device_id", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_MissingPayload_ReturnsFail()
    {
        var envelope = CreateValidEnvelope() with { Payload = null };
        var result = TelemetryValidator.Validate(envelope);
        Assert.False(result.Success);
        Assert.Equal("missing_payload", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_ValidPayload_ReturnsSuccess()
    {
        var payload = new EnvironmentTelemetryPayload { TemperatureC = 22.5 };
        var result = TelemetryValidator.ValidatePayload(payload);
        Assert.True(result.Success);
    }

    [Fact]
    public void TelemetryValidator_InvalidTemperature_ReturnsFail()
    {
        var payload = new EnvironmentTelemetryPayload { TemperatureC = 100.0 };
        var result = TelemetryValidator.ValidatePayload(payload);
        Assert.False(result.Success);
        Assert.Equal("invalid_temperature", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_InvalidHumidity_ReturnsFail()
    {
        var payload = new EnvironmentTelemetryPayload { RelativeHumidityPct = 150.0 };
        var result = TelemetryValidator.ValidatePayload(payload);
        Assert.False(result.Success);
        Assert.Equal("invalid_humidity", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_InvalidCo2_ReturnsFail()
    {
        var payload = new EnvironmentTelemetryPayload { Co2Ppm = 20000.0 };
        var result = TelemetryValidator.ValidatePayload(payload);
        Assert.False(result.Success);
        Assert.Equal("invalid_co2", result.ErrorCode);
    }

    [Fact]
    public void TelemetryValidator_EmptyPayload_ReturnsFail()
    {
        var payload = new EnvironmentTelemetryPayload();
        var result = TelemetryValidator.ValidatePayload(payload);
        Assert.False(result.Success);
        Assert.Equal("empty_payload", result.ErrorCode);
    }

    [Fact]
    public void TelemetryTopicParser_ValidTopic_ReturnsTrue()
    {
        var buildingId = Guid.NewGuid().ToString();
        var deviceId = Guid.NewGuid().ToString();
        var topic = $"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment";

        var result = TelemetryTopicParser.TryParseTopic(topic, out var parsedBuilding, out var parsedDevice);

        Assert.True(result);
        Assert.Equal(buildingId, parsedBuilding);
        Assert.Equal(deviceId, parsedDevice);
    }

    [Fact]
    public void TelemetryTopicParser_InvalidTopic_ReturnsFalse()
    {
        var result = TelemetryTopicParser.TryParseTopic("invalid/topic", out _, out _);
        Assert.False(result);
    }

    [Fact]
    public void CommandTransitions_ValidTransition_Succeeds()
    {
        Assert.True(CommandTransitions.IsValid(CommandStatus.Created, CommandStatus.Validated));
        Assert.True(CommandTransitions.IsValid(CommandStatus.Validated, CommandStatus.Queued));
        Assert.True(CommandTransitions.IsValid(CommandStatus.Queued, CommandStatus.Published));
        Assert.True(CommandTransitions.IsValid(CommandStatus.Published, CommandStatus.Acknowledged));
        Assert.True(CommandTransitions.IsValid(CommandStatus.Acknowledged, CommandStatus.Executing));
        Assert.True(CommandTransitions.IsValid(CommandStatus.Executing, CommandStatus.Succeeded));
    }

    [Fact]
    public void CommandTransitions_InvalidTransition_Fails()
    {
        Assert.False(CommandTransitions.IsValid(CommandStatus.Created, CommandStatus.Succeeded));
        Assert.False(CommandTransitions.IsValid(CommandStatus.Published, CommandStatus.Succeeded));
        Assert.False(CommandTransitions.IsValid(CommandStatus.Succeeded, CommandStatus.Failed));
    }

    [Fact]
    public void CommandTransitions_TerminalStates_AreCorrect()
    {
        Assert.True(CommandTransitions.IsTerminal(CommandStatus.Succeeded));
        Assert.True(CommandTransitions.IsTerminal(CommandStatus.Failed));
        Assert.True(CommandTransitions.IsTerminal(CommandStatus.Expired));
        Assert.True(CommandTransitions.IsTerminal(CommandStatus.Cancelled));
        Assert.True(CommandTransitions.IsTerminal(CommandStatus.Rejected));
        Assert.True(CommandTransitions.IsTerminal(CommandStatus.TimedOut));
        Assert.False(CommandTransitions.IsTerminal(CommandStatus.Created));
        Assert.False(CommandTransitions.IsTerminal(CommandStatus.Published));
    }

    [Fact]
    public void Command_FullLifeCycle_ProducesCorrectTransitions()
    {
        var cmdId = CommandId.New();
        var buildingId = BuildingId.From(Guid.NewGuid());
        var deviceId = DeviceId.From(Guid.NewGuid());

        var cmd = Command.Create(cmdId, buildingId, deviceId, "actuate.relay.on", "on", "{}");

        Assert.Equal(CommandStatus.Created, cmd.Status);

        cmd.Validate();
        Assert.Equal(CommandStatus.Validated, cmd.Status);

        cmd.Queue();
        Assert.Equal(CommandStatus.Queued, cmd.Status);

        cmd.MarkPublished();
        Assert.Equal(CommandStatus.Published, cmd.Status);

        cmd.Acknowledge();
        Assert.Equal(CommandStatus.Acknowledged, cmd.Status);

        cmd.StartExecution();
        Assert.Equal(CommandStatus.Executing, cmd.Status);

        cmd.CompleteSuccessfully();
        Assert.Equal(CommandStatus.Succeeded, cmd.Status);
    }

    [Fact]
    public void Command_FailurePath_ProducesFailedStatus()
    {
        var cmd = CreateTestCommand();
        cmd.Validate();
        cmd.Queue();
        cmd.MarkPublished();
        cmd.Acknowledge();
        cmd.StartExecution();
        cmd.CompleteWithFailure("HARDWARE_ERROR", "Device not responding");

        Assert.Equal(CommandStatus.Failed, cmd.Status);
        Assert.Equal("HARDWARE_ERROR", cmd.LastErrorCode);
    }

    [Fact]
    public void Command_Timeout_ProducesTimedOutStatus()
    {
        var cmd = CreateTestCommand();
        cmd.Validate();
        cmd.Queue();
        cmd.MarkPublished();
        cmd.Timeout();

        Assert.Equal(CommandStatus.TimedOut, cmd.Status);
        Assert.Equal("EXECUTION_TIMEOUT", cmd.LastErrorCode);
    }

    [Fact]
    public void Command_Cancel_ProducesCancelledStatus()
    {
        var cmd = CreateTestCommand();
        cmd.Cancel();
        Assert.Equal(CommandStatus.Cancelled, cmd.Status);
    }

    [Fact]
    public void Command_Reject_ProducesRejectedStatus()
    {
        var cmd = CreateTestCommand();
        cmd.Reject("INVALID_CAPABILITY", "Device lacks required capability");
        Assert.Equal(CommandStatus.Rejected, cmd.Status);
    }

    [Fact]
    public void Command_Expire_ProducesExpiredStatus()
    {
        var cmd = CreateTestCommand();
        cmd.Validate();
        cmd.Queue();
        cmd.MarkPublished();
        cmd.Expire();
        Assert.Equal(CommandStatus.Expired, cmd.Status);
    }

    [Fact]
    public void Command_VersionIncrements_OnTransition()
    {
        var cmd = CreateTestCommand();
        var v1 = cmd.Version;
        cmd.Validate();
        cmd.Queue();
        Assert.True(cmd.Version > v1);
    }

    [Fact]
    public void QualityClassifier_TemperatureOnly_ReturnsNormal()
    {
        var payload = new EnvironmentTelemetryPayload { TemperatureC = 22.5 };
        var quality = MeasurementQualityClassifier.Classify(payload);
        Assert.Equal("normal", quality);
    }

    [Fact]
    public void QualityClassifier_AllGoodValues_ReturnsNormal()
    {
        var payload = new EnvironmentTelemetryPayload { TemperatureC = 22.5, RelativeHumidityPct = 50.0, Co2Ppm = 600.0 };
        var quality = MeasurementQualityClassifier.Classify(payload);
        Assert.Equal("normal", quality);
    }

    [Fact]
    public void QualityClassifier_ExtremeTemperature_ReturnsSuspect()
    {
        var payload = new EnvironmentTelemetryPayload { TemperatureC = 50.0 };
        var quality = MeasurementQualityClassifier.Classify(payload);
        Assert.Equal("suspect", quality);
    }

    [Fact]
    public void TelemetryTopicParser_CommandAckTopic_ForTelemetry_ReturnsFalse()
    {
        var result = TelemetryTopicParser.TryParseTopic("climate-hub/v1/guid/guid/command/ack", out _, out _);
        Assert.False(result);
    }

    [Fact]
    public void Command_CreateWithRoomId_SetsRoomCorrectly()
    {
        var roomId = RoomId.From(Guid.NewGuid());
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()),
            "actuate.fan.on", "on", "{}", roomId: roomId);
        Assert.Equal(roomId, cmd.RoomId);
    }

    [Fact]
    public void Command_CreateWithPriority_SetsPriorityCorrectly()
    {
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()),
            "actuate.valve.open", "open", "{}", CommandPriority.High);
        Assert.Equal(CommandPriority.High, cmd.Priority);
    }

    private static Command CreateTestCommand()
    {
        return Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()),
            DeviceId.From(Guid.NewGuid()), "actuate.relay.on", "on", "{}");
    }

    private static EnvironmentTelemetryEnvelope CreateValidEnvelope()
    {
        return new EnvironmentTelemetryEnvelope
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageType = "environment.telemetry",
            ProtocolVersion = "1.0",
            BuildingId = Guid.NewGuid().ToString(),
            DeviceId = Guid.NewGuid().ToString(),
            BootId = Guid.NewGuid().ToString(),
            SequenceNumber = 1,
            MeasuredAt = DateTimeOffset.UtcNow.ToString("O"),
            Payload = JsonSerializer.SerializeToElement(new { temperatureC = 22.5, relativeHumidityPct = 45.0 })
        };
    }
}

public record EnvironmentTelemetryEnvelope
{
    public string MessageId { get; init; } = "";
    public string MessageType { get; init; } = "";
    public string ProtocolVersion { get; init; } = "";
    public string BuildingId { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public string BootId { get; init; } = "";
    public int SequenceNumber { get; init; }
    public string MeasuredAt { get; init; } = "";
    public JsonElement? Payload { get; init; }
}

public record EnvironmentTelemetryPayload
{
    public double? TemperatureC { get; init; }
    public double? RelativeHumidityPct { get; init; }
    public double? Co2Ppm { get; init; }
}

public static class TelemetryTopicParser
{
    public static bool TryParseTopic(string topic, out string buildingId, out string deviceId)
    {
        buildingId = "";
        deviceId = "";
        var parts = topic.Split('/');
        if (parts.Length < 6 || parts[0] != "climate-hub" || parts[1] != "v1")
            return false;
        buildingId = parts[2];
        deviceId = parts[3];
        return parts[4] == "telemetry" && parts[5] == "environment";
    }
}

public static class TelemetryValidator
{
    public static TelemetryParseResult Validate(EnvironmentTelemetryEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.MessageId))
            return Fail("missing_message_id", "messageId is required");
        if (string.IsNullOrWhiteSpace(envelope.ProtocolVersion))
            return Fail("missing_protocol_version", "protocolVersion is required");
        if (!envelope.ProtocolVersion.StartsWith("1."))
            return Fail("unsupported_protocol_version", $"protocolVersion '{envelope.ProtocolVersion}' is not supported. Expected 1.x");
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

    private static TelemetryParseResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

public class TelemetryParseResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public static class MeasurementQualityClassifier
{
    public static string Classify(EnvironmentTelemetryPayload payload)
    {
        if (payload.TemperatureC is > 45 or < -10) return "suspect";
        if (payload.RelativeHumidityPct is > 95 or < 5) return "suspect";
        if (payload.Co2Ppm is > 8000) return "suspect";
        return "normal";
    }
}