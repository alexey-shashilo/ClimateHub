using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.EndToEndTests;

public class TestActuatorRuntime : IAsyncDisposable
{
    private readonly IMqttClient _subscriber;
    private readonly IMqttClient _publisher;
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly string _buildingId;
    private readonly string _deviceId;
    private readonly string _clientId;

    public string DeviceId => _deviceId;
    public string BuildingId => _buildingId;

    private readonly List<CapturedCommand> _receivedCommands = new();
    private readonly List<PublishedTelemetry> _publishedTelemetries = new();
    private readonly object _lock = new();

    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TestActuatorRuntime(string mqttHost, int mqttPort, string buildingId, string deviceId)
    {
        _mqttHost = mqttHost;
        _mqttPort = mqttPort;
        _buildingId = buildingId;
        _deviceId = deviceId;
        _clientId = $"act-{deviceId[..8]}-{Guid.NewGuid():N}"[..20];

        var factory = new MqttClientFactory();
        _subscriber = factory.CreateMqttClient();
        _publisher = factory.CreateMqttClient();

        _publisher.ConnectedAsync += _ => { _ready.TrySetResult(); return Task.CompletedTask; };
    }

    public async Task StartAsync()
    {
        var subOpts = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"sub-{_clientId}")
            .WithCleanSession()
            .Build();

        var pubOpts = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"pub-{_clientId}")
            .WithCleanSession()
            .Build();

        await _subscriber.ConnectAsync(subOpts);
        await _publisher.ConnectAsync(pubOpts);
        await _ready.Task;

        _subscriber.ApplicationMessageReceivedAsync += OnMessageReceived;

        await _subscriber.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter($"climate-hub/v1/{_buildingId}/{_deviceId}/command/execute", MqttQualityOfServiceLevel.AtLeastOnce)
            .WithTopicFilter($"climate-hub/v1/{_buildingId}/{_deviceId}/command/cancel", MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            await HandleCommandAsync(e.ApplicationMessage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Actuator] ERROR: {ex.Message}");
        }
    }

    private async Task HandleCommandAsync(MqttApplicationMessage msg)
    {
        var payload = Encoding.UTF8.GetString(msg.Payload.FirstSpan);
        var parts = msg.Topic.Split('/');

        var commandId = ExtractCommandId(payload);
        if (string.IsNullOrEmpty(commandId))
            return;

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

        await PublishEvent("ack", new { commandId });
        await Task.Delay(200);
        await PublishEvent("progress", new { commandId });
        await Task.Delay(300);

        var (parameter, value) = SimulateEffect(payload);

        await PublishEvent("result", new
        {
            commandId,
            status = "succeeded",
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
            reportedState = new { state = "on", value, changedAt = DateTimeOffset.UtcNow.ToString("O") }
        });

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
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        lock (_lock)
        {
            _publishedTelemetries.Add(new PublishedTelemetry
            {
                Parameter = parameter, Value = value,
                PublishedAt = DateTimeOffset.UtcNow, Success = pubResult.IsSuccess
            });
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
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());
    }

    public IReadOnlyList<CapturedCommand> ReceivedCommands
    {
        get { lock (_lock) return _receivedCommands.ToList(); }
    }

    public async ValueTask DisposeAsync()
    {
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