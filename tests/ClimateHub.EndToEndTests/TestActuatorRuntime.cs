using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.EndToEndTests;

public enum ActuatorMode
{
    Successful,
    Rejected,
    Cancelled,
    DuplicateAck,
    LateAck,
    NoAck,
    CommandFailed,
    CommandTimeout,
    CapabilityUnsupported,
    DeviceOffline,
    LateTelemetry
}

public class TestActuatorRuntime : IAsyncDisposable
{
    private readonly IMqttClient _subscriber;
    private readonly IMqttClient _publisher;
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly string _buildingId;
    private readonly string _deviceId;
    private readonly string _clientId;
    private readonly ActuatorMode _mode;
    private readonly TimeSpan _simulatedLatency;
    private bool _disposed;

    public string DeviceId => _deviceId;
    public string BuildingId => _buildingId;

    private readonly List<CapturedCommand> _receivedCommands = new();
    private readonly List<PublishedTelemetry> _publishedTelemetries = new();
    private readonly object _lock = new();
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TestActuatorRuntime(string mqttHost, int mqttPort, string buildingId, string deviceId,
        ActuatorMode mode = ActuatorMode.Successful, TimeSpan? simulatedLatency = null)
    {
        _mqttHost = mqttHost;
        _mqttPort = mqttPort;
        _buildingId = buildingId;
        _deviceId = deviceId;
        _mode = mode;
        _simulatedLatency = simulatedLatency ?? TimeSpan.FromMilliseconds(200);
        _clientId = $"act-{deviceId[..8]}-{Guid.NewGuid():N}"[..20];

        var factory = new MqttClientFactory();
        _subscriber = factory.CreateMqttClient();
        _publisher = factory.CreateMqttClient();

        if (mode == ActuatorMode.DeviceOffline)
            return;

        _publisher.ConnectedAsync += _ => { _ready.TrySetResult(); return Task.CompletedTask; };
    }

    public TaskCompletionSource CommandReceived => new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task StartAsync()
    {
        if (_mode == ActuatorMode.DeviceOffline)
            return;

        var subOpts = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"sub-{_clientId}").WithCleanSession().Build();
        var pubOpts = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"pub-{_clientId}").WithCleanSession().Build();

        await _subscriber.ConnectAsync(subOpts);
        await _publisher.ConnectAsync(pubOpts);
        await _ready.Task;

        _subscriber.ApplicationMessageReceivedAsync += OnMessageReceived;

        await _subscriber.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"climate-hub/v1/{_buildingId}/{_deviceId}/command/execute", MqttQualityOfServiceLevel.AtLeastOnce)
            .WithTopicFilter($"climate-hub/v1/{_buildingId}/{_deviceId}/command/cancel", MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
    }

    public async Task RespondToLastCommand(string overrideStatus = "succeeded")
    {
        var cmd = ReceivedCommands.LastOrDefault();
        if (cmd is null) return;
        await _simulatedLatency.Delay();
        await PublishEvent("result", new
        {
            commandId = cmd.CommandId,
            status = overrideStatus,
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
            reportedState = new { state = "on", value = 22.5, changedAt = DateTimeOffset.UtcNow.ToString("O") }
        });
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try { await HandleCommandAsync(e.ApplicationMessage); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Actuator] ERROR: {ex.Message}"); }
    }

    private async Task HandleCommandAsync(MqttApplicationMessage msg)
    {
        var payload = Encoding.UTF8.GetString(msg.Payload.FirstSpan);
        var commandId = ExtractCommandId(payload);
        if (string.IsNullOrEmpty(commandId)) return;

        lock (_lock)
        {
            _receivedCommands.Add(new CapturedCommand
            {
                Topic = msg.Topic,
                Payload = payload,
                CommandId = commandId,
                ReceivedAt = DateTimeOffset.UtcNow
            });
        }

        CommandReceived.TrySetResult();

        switch (_mode)
        {
            case ActuatorMode.Rejected:
                await PublishEvent("error", new { commandId, errorCode = "CAPABILITY_UNSUPPORTED", errorMessage = "Capability not supported by this device" });
                break;

            case ActuatorMode.Cancelled:
                await PublishEvent("result", new { commandId, status = "cancelled", timestamp = DateTimeOffset.UtcNow.ToString("O") });
                break;

            case ActuatorMode.CommandFailed:
                await PublishEvent("ack", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("progress", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("result", new
                {
                    commandId,
                    status = "failed",
                    errorCode = "HARDWARE_ERROR",
                    errorMessage = "Device reported hardware failure",
                    timestamp = DateTimeOffset.UtcNow.ToString("O")
                });
                break;

            case ActuatorMode.LateTelemetry:
                await PublishEvent("ack", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("progress", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("result", new
                {
                    commandId,
                    status = "succeeded",
                    timestamp = DateTimeOffset.UtcNow.ToString("O"),
                    reportedState = new { state = "on", value = 22.5, changedAt = DateTimeOffset.UtcNow.ToString("O") }
                });
                await Task.Delay(5000); // Late telemetry after significant delay
                await PublishEffectTelemetry(payload);
                break;

            case ActuatorMode.LateAck:
                // No immediate ACK
                await Task.Delay(3000);
                await PublishEvent("ack", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("progress", new { commandId });
                await _simulatedLatency.Delay();
                await PublishNormalCompletion(commandId, payload);
                break;

            case ActuatorMode.NoAck:
                await PublishEvent("progress", new { commandId });
                await _simulatedLatency.Delay();
                await PublishNormalCompletion(commandId, payload);
                break;

            case ActuatorMode.CommandTimeout:
                await PublishEvent("ack", new { commandId });
                // No progress, no result — let the timeout worker handle it
                break;

            case ActuatorMode.CapabilityUnsupported:
                await PublishEvent("ack", new { commandId });
                await PublishEvent("result", new
                {
                    commandId,
                    status = "failed",
                    errorCode = "CAPABILITY_UNSUPPORTED",
                    errorMessage = "This device does not support the requested capability",
                    timestamp = DateTimeOffset.UtcNow.ToString("O")
                });
                break;

            case ActuatorMode.DuplicateAck:
                await PublishEvent("ack", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("ack", new { commandId }); // Duplicate ACK
                await _simulatedLatency.Delay();
                await PublishEvent("progress", new { commandId });
                await _simulatedLatency.Delay();
                await PublishNormalCompletion(commandId, payload);
                break;

            default: // Successful
                await PublishEvent("ack", new { commandId });
                await _simulatedLatency.Delay();
                await PublishEvent("progress", new { commandId });
                await _simulatedLatency.Delay();
                await PublishNormalCompletion(commandId, payload);
                break;
        }
    }

    private async Task PublishNormalCompletion(string commandId, string payload)
    {
        var (parameter, value) = SimulateEffect(payload);

        await PublishEvent("result", new
        {
            commandId,
            status = "succeeded",
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
            reportedState = new { state = "on", value, changedAt = DateTimeOffset.UtcNow.ToString("O") }
        });

        await PublishEffectTelemetry(payload);
    }

    private async Task PublishEffectTelemetry(string commandPayload)
    {
        var (parameter, value) = SimulateEffect(commandPayload);
        var telemetryPayload = new Dictionary<string, object> { [parameter] = value };

        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId = _buildingId,
            deviceId = _deviceId,
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = Random.Shared.Next(1, 100000),
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = telemetryPayload
        });

        var pubResult = await _publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{_buildingId}/{_deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());

        lock (_lock)
        {
            _publishedTelemetries.Add(new PublishedTelemetry
            { Parameter = parameter, Value = value, PublishedAt = DateTimeOffset.UtcNow, Success = pubResult.IsSuccess });
        }
    }

    private static string ExtractCommandId(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.TryGetProperty("commandId", out var cid) && cid.ValueKind == JsonValueKind.String)
                return cid.GetString() ?? "";
            if (root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString() ?? "";
        }
        catch { }
        return "";
    }

    private static (string parameter, double value) SimulateEffect(string commandPayload)
    {
        try
        {
            using var doc = JsonDocument.Parse(commandPayload);
            var root = doc.RootElement;
            string? capabilityCode = null;
            if (root.TryGetProperty("capabilityCode", out var cap))
                capabilityCode = cap.GetString();
            else if (root.TryGetProperty("payload", out var p) && p.ValueKind == JsonValueKind.Object)
            {
                if (p.TryGetProperty("capabilityCode", out var cap2))
                    capabilityCode = cap2.GetString();
            }

            return capabilityCode switch
            {
                "eng.temperature.increase" or "actuate.relay.on" or "control.valve" => ("temperatureC", 23.5),
                "eng.temperature.decrease" or "actuate.fan.on" or "control.fan-speed" => ("temperatureC", 21.0),
                "eng.humidity.increase" or "control.humidifier" => ("relativeHumidityPct", 55.0),
                "eng.co2.reduce" or "control.damper-position" => ("co2Ppm", 450),
                _ => ("temperatureC", 22.5)
            };
        }
        catch { return ("temperatureC", 22.5); }
    }

    private async Task PublishEvent(string eventType, object payload)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = $"command.{eventType}",
            protocolVersion = "1.0",
            buildingId = _buildingId,
            deviceId = _deviceId,
            commandId = "",
            attemptNumber = 1,
            createdAt = DateTimeOffset.UtcNow.ToString("O"),
            payload
        });

        await _publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{_buildingId}/{_deviceId}/command/{eventType}")
            .WithPayload(Encoding.UTF8.GetBytes(envelope))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
    }

    public IReadOnlyList<CapturedCommand> ReceivedCommands
    {
        get { lock (_lock) return _receivedCommands.ToList(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_subscriber?.IsConnected == true) await _subscriber.DisconnectAsync();
            if (_publisher?.IsConnected == true) await _publisher.DisconnectAsync();
            _subscriber?.Dispose();
            _publisher?.Dispose();
        }
        catch { }
    }

    public record CapturedCommand
    {
        public string Topic { get; init; } = "";
        public string Payload { get; init; } = "";
        public string CommandId { get; init; } = "";
        public DateTimeOffset ReceivedAt { get; init; }
    }

    public record PublishedTelemetry
    {
        public string Parameter { get; init; } = "";
        public double Value { get; init; }
        public DateTimeOffset PublishedAt { get; init; }
        public bool Success { get; init; }
    }
}

internal static class TimeSpanExtensions
{
    public static Task Delay(this TimeSpan span) => Task.Delay(span);
}
