using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.DeviceSimulator;

public sealed class SimulatorHost : IAsyncDisposable
{
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly Guid _buildingId;
    private readonly Guid _deviceId;
    private readonly int _intervalMs;
    private readonly bool _resendDuplicates;
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _mqttOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    private Guid _bootId;
    private uint _sequenceNumber;
    private double _temperature = 22.5;
    private double _humidity = 42.0;
    private double _co2 = 700;
    private bool _sentDuplicate;
    private CancellationTokenSource? _cts;

    public SimulatorHost(string[] args)
    {
        _mqttHost = GetArg(args, "--mqtt-host", "localhost");
        _mqttPort = int.Parse(GetArg(args, "--mqtt-port", "1883"));
        _buildingId = Guid.Parse(GetArg(args, "--building-id", Guid.NewGuid().ToString()));
        _deviceId = Guid.Parse(GetArg(args, "--device-id", Guid.NewGuid().ToString()));
        _intervalMs = int.Parse(GetArg(args, "--interval", "5000"));
        _resendDuplicates = args.Contains("--resend-duplicate");

        _bootId = Guid.NewGuid();
        _sequenceNumber = 0;

        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttHost, _mqttPort)
            .WithClientId($"simulator-{_deviceId:N}")
            .WithCleanSession()
            .Build();

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
        };

        Console.WriteLine("Climate Hub Device Simulator");
        Console.WriteLine($"  Building ID: {_buildingId}");
        Console.WriteLine($"  Device ID:   {_deviceId}");
        Console.WriteLine($"  Boot ID:     {_bootId}");
        Console.WriteLine($"  MQTT:        {_mqttHost}:{_mqttPort}");
        Console.WriteLine($"  Interval:    {_intervalMs}ms");
        Console.WriteLine($"  Duplicate test: {_resendDuplicates}");
        Console.WriteLine("Press Ctrl+C to stop.\n");

        await ConnectAsync(_cts.Token);

        while (!_cts.Token.IsCancellationRequested)
        {
            await SendTelemetryAsync(_cts.Token);
            await Task.Delay(_intervalMs, _cts.Token);
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var connectResult = await _mqttClient.ConnectAsync(_mqttOptions, ct);
        Console.WriteLine($"Connected: {connectResult.ResultCode}");
    }

    private async Task SendTelemetryAsync(CancellationToken ct)
    {
        _sequenceNumber++;
        var messageId = Guid.NewGuid();

        _temperature += (Random.Shared.NextDouble() - 0.5) * 0.2;
        _temperature = Math.Clamp(_temperature, 18, 30);
        _humidity += (Random.Shared.NextDouble() - 0.5) * 2;
        _humidity = Math.Clamp(_humidity, 20, 80);
        _co2 += (Random.Shared.NextDouble() - 0.5) * 50;
        _co2 = Math.Clamp(_co2, 350, 2000);

        var telemetry = new
        {
            messageId = messageId.ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId = _buildingId.ToString(),
            deviceId = _deviceId.ToString(),
            bootId = _bootId.ToString(),
            sequenceNumber = _sequenceNumber,
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new
            {
                temperatureC = Math.Round(_temperature, 1),
                relativeHumidityPct = Math.Round(_humidity, 1),
                co2Ppm = Math.Round(_co2, 0)
            }
        };

        var jsonPayload = JsonSerializer.Serialize(telemetry, _jsonOptions);
        var topic = $"climate-hub/v1/{_buildingId}/{_deviceId}/telemetry/environment";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(jsonPayload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var publishResult = await _mqttClient.PublishAsync(message, ct);

        Console.WriteLine(
            "[{0:HH:mm:ss}] Seq={1} T={2:F1} C H={3:F1}% CO2={4:F0}ppm -> {5}",
            DateTimeOffset.UtcNow,
            _sequenceNumber,
            telemetry.payload.temperatureC,
            telemetry.payload.relativeHumidityPct,
            telemetry.payload.co2Ppm,
            publishResult.ReasonCode);

        if (_resendDuplicates && !_sentDuplicate)
        {
            _sentDuplicate = true;
            await Task.Delay(500, ct);
            var dupResult = await _mqttClient.PublishAsync(message, ct);
            Console.WriteLine(
                "[{0:HH:mm:ss}] DUPLICATE Seq={1} -> {2}",
                DateTimeOffset.UtcNow,
                _sequenceNumber,
                dupResult.ReasonCode);
        }
    }

    private static string GetArg(string[] args, string name, string defaultValue)
    {
        var index = Array.IndexOf(args, name);
        if (index >= 0 && index + 1 < args.Length)
            return args[index + 1];
        return defaultValue;
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
        }
        _mqttClient.Dispose();
        _cts?.Dispose();
    }
}