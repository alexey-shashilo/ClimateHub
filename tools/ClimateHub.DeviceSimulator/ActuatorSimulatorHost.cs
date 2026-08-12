using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.DeviceSimulator;

public class ActuatorSimulatorHost : IAsyncDisposable
{
    private readonly IMqttClient _mqttClient;
    private readonly Guid _buildingId;
    private readonly Guid _deviceId;
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly Dictionary<string, string> _reportedState = new();
    private readonly HashSet<string> _processedCommandIds = new();
    private readonly Random _rng = new();
    private CancellationTokenSource? _cts;

    private int _ackDelayMs;
    private int _executionDelayMs;
    private int _progressIntervalMs;
    private bool _rejectMode;
    private bool _failMode;
    private bool _ignoreMode;

    public ActuatorSimulatorHost(string[] args)
    {
        _mqttClient = new MqttClientFactory().CreateMqttClient();
        _buildingId = Guid.Parse(GetArg(args, "--building-id", Guid.NewGuid().ToString()));
        _deviceId = Guid.Parse(GetArg(args, "--device-id", Guid.NewGuid().ToString()));
        _ackDelayMs = int.Parse(GetArg(args, "--ack-delay", "500"));
        _executionDelayMs = int.Parse(GetArg(args, "--execution-delay", "2000"));
        _progressIntervalMs = int.Parse(GetArg(args, "--progress-interval", "0"));
        _rejectMode = args.Contains("--reject");
        _failMode = args.Contains("--fail");
        _ignoreMode = args.Contains("--ignore");

        var host = GetArg(args, "--mqtt-host", "localhost");
        var port = int.Parse(GetArg(args, "--mqtt-port", "1883"));

        _mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId($"actuator-{_deviceId:N}")
            .WithCleanSession().Build();
    }

    private MqttClientOptions _mqttOptions;

    public async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; _cts.Cancel(); };
        Console.WriteLine("Climate Hub Actuator Simulator");
        Console.WriteLine($"  Building: {_buildingId}");
        Console.WriteLine($"  Device:   {_deviceId}");
        Console.WriteLine($"  Modes:    reject={_rejectMode} fail={_failMode} ignore={_ignoreMode}");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        await _mqttClient.ConnectAsync(_mqttOptions, _cts.Token);
        Console.WriteLine("Connected");

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.FirstSpan);
            await HandleCommandAsync(topic, payload, _cts.Token);
        };

        var topicFilter = $"climate-hub/v1/{_buildingId}/{_deviceId}/command/+";
        await _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(topicFilter).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build(), _cts.Token);
        Console.WriteLine($"Subscribed to {topicFilter}");

        try { await Task.Delay(Timeout.Infinite, _cts.Token); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleCommandAsync(string topic, string payload, CancellationToken ct)
    {
        var parts = topic.Split('/');
        var commandType = parts.Length >= 6 ? parts[5] : "";
        if (commandType != "execute") return;

        if (_ignoreMode) { Console.WriteLine("  IGNORE mode — skipping"); return; }

        CommandExecuteEnvelope? envelope;
        try { envelope = JsonSerializer.Deserialize<CommandExecuteEnvelope>(payload); }
        catch { Console.WriteLine("  Invalid execute payload"); return; }
        if (envelope is null) return;

        var cmdId = envelope.CommandId;

        // Duplicate check
        if (_processedCommandIds.Contains(cmdId))
        {
            Console.WriteLine($"  DUPLICATE command {cmdId} — sending duplicate ack");
            await PublishEventAsync("ack", cmdId, envelope, new { status = "duplicate" }, ct);
            return;
        }
        _processedCommandIds.Add(cmdId);

        Console.WriteLine($"  EXECUTE cmd={cmdId} cap={envelope.Payload?.capabilityCode} op={envelope.Payload?.operation}");

        // Ack
        if (_rejectMode)
        {
            await PublishEventAsync("ack", cmdId, envelope, new { status = "rejected", errorCode = "CAPABILITY_NOT_SUPPORTED" }, ct);
            return;
        }

        await Task.Delay(_ackDelayMs, ct);
        await PublishEventAsync("ack", cmdId, envelope, new { status = "accepted" }, ct);

        // Progress
        if (_progressIntervalMs > 0)
        {
            for (int pct = 25; pct <= 75; pct += 25)
            {
                await Task.Delay(_progressIntervalMs, ct);
                await PublishEventAsync("progress", cmdId, envelope, new { progressPct = pct, stage = "applying" }, ct);
            }
        }

        await Task.Delay(_executionDelayMs, ct);

        // Result
        if (_failMode)
        {
            var reported = BuildReportedState(envelope);
            await PublishEventAsync("result", cmdId, envelope, new { status = "failed", errorCode = "ACTUATOR_BLOCKED", errorMessage = "Simulated failure", reportedState = reported }, ct);
            Console.WriteLine($"  FAILED cmd={cmdId}");
        }
        else
        {
            var reported = BuildReportedState(envelope);
            _reportedState[envelope.Payload?.capabilityCode ?? ""] = JsonSerializer.Serialize(reported);
            await PublishEventAsync("result", cmdId, envelope, new { status = "succeeded", reportedState = reported }, ct);
            Console.WriteLine($"  SUCCEEDED cmd={cmdId}");
        }
    }

    private async Task PublishEventAsync(string eventType, string cmdId, CommandExecuteEnvelope env, object payload, CancellationToken ct)
    {
        var topic = $"climate-hub/v1/{_buildingId}/{_deviceId}/command/{eventType}";
        var msg = new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = $"command.{eventType}",
            protocolVersion = "1.0",
            buildingId = _buildingId.ToString(),
            deviceId = _deviceId.ToString(),
            commandId = cmdId,
            attemptNumber = 1,
            createdAt = DateTimeOffset.UtcNow,
            payload
        };
        var body = JsonSerializer.Serialize(msg, _json);
        var mqttMsg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(body))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        await _mqttClient.PublishAsync(mqttMsg, ct);
        Console.WriteLine($"  -> {eventType}");
    }

    private object BuildReportedState(CommandExecuteEnvelope env)
    {
        var cap = env.Payload?.capabilityCode ?? "";
        return cap switch
        {
            "control.relay" => new { enabled = true },
            "control.fan-speed" => new { speedPct = 60 },
            "control.damper-position" => new { positionPct = 35 },
            _ => new { }
        };
    }

    private static string GetArg(string[] args, string name, string defaultValue)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : defaultValue;
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_mqttClient.IsConnected) await _mqttClient.DisconnectAsync();
        _mqttClient.Dispose();
        _cts?.Dispose();
    }
}

public class CommandExecuteEnvelope
{
    public string MessageId { get; set; } = "";
    public string MessageType { get; set; } = "";
    public string ProtocolVersion { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string CommandId { get; set; } = "";
    public int AttemptNumber { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public CommandPayload? Payload { get; set; }
}

public class CommandPayload
{
    public string capabilityCode { get; set; } = "";
    public string operation { get; set; } = "";
    public string contractVersion { get; set; } = "";
    public JsonElement? parameters { get; set; }
}
