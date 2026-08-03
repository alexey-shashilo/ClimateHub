using System.Text;
using System.Threading.Channels;
using ClimateHub.DeviceGateway.Services;
using ClimateHub.SharedKernel.Configuration;
using MQTTnet;
using MQTTnet.Protocol;
using Microsoft.Extensions.Options;

namespace ClimateHub.DeviceGateway;

public class DeviceGatewayWorker : BackgroundService
{
    private readonly ILogger<DeviceGatewayWorker> _logger;
    private readonly IOptions<MqttOptions> _mqttOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Channel<(string topic, string payload, string type)> _telemetryChannel;
    private readonly Channel<(string topic, string payload, string type)> _commandChannel;
    private IMqttClient? _mqttClient;
    private const int ChannelCapacity = 1000;

    public DeviceGatewayWorker(
        ILogger<DeviceGatewayWorker> logger,
        IOptions<MqttOptions> mqttOptions,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _mqttOptions = mqttOptions;
        _scopeFactory = scopeFactory;
        _telemetryChannel = Channel.CreateBounded<(string, string, string)>(new BoundedChannelOptions(ChannelCapacity)
        { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
        _commandChannel = Channel.CreateBounded<(string, string, string)>(new BoundedChannelOptions(100)
        { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Gateway starting");

        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttOptions.Value.Host, _mqttOptions.Value.Port)
            .WithClientId(_mqttOptions.Value.ClientId ?? "climate-hub-device-gateway")
            .WithCleanSession().Build();

        _mqttClient.ConnectedAsync += async e =>
        {
            _logger.LogInformation("Connected: {Result}", e.ConnectResult.ResultCode);
            await SubscribeAsync(stoppingToken);
        };

        _mqttClient.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("Disconnected: {Reason}", e.Reason);
            if (!stoppingToken.IsCancellationRequested)
                await ConnectWithRetryAsync(options, stoppingToken);
        };

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.FirstSpan);

            if (topic.Contains("/telemetry/"))
                _telemetryChannel.Writer.TryWrite((topic, payload, "telemetry"));
            else if (topic.Contains("/command/"))
                _commandChannel.Writer.TryWrite((topic, payload, "command"));
            return Task.CompletedTask;
        };

        var telemetryTask = ConsumeTelemetryAsync(stoppingToken);
        var commandTask = ConsumeCommandsAsync(stoppingToken);
        await ConnectWithRetryAsync(options, stoppingToken);
        await Task.WhenAll(telemetryTask, commandTask);
    }

    private async Task ConsumeTelemetryAsync(CancellationToken ct)
    {
        await foreach (var (topic, payload, _) in _telemetryChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<TelemetryIngestionHandler>();
                await handler.HandleAsync(topic, payload, ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Telemetry error"); }
        }
    }

    private async Task ConsumeCommandsAsync(CancellationToken ct)
    {
        await foreach (var (topic, payload, _) in _commandChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<CommandEventConsumer>();
                await consumer.HandleAsync(topic, payload, ct);
            }
            catch (Exception ex) { _logger.LogError(ex, "Command event error"); }
        }
    }

    private async Task ConnectWithRetryAsync(MqttClientOptions options, CancellationToken ct)
    {
        var delay = _mqttOptions.Value.ReconnectBaseDelayMs;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _mqttClient!.ConnectAsync(options, ct);
                if (result.ResultCode == MqttClientConnectResultCode.Success) return;
            }
            catch { _logger.LogWarning("MQTT connect failed, retrying in {Delay}ms", delay); }
            await Task.Delay(delay, ct);
            delay = Math.Min(delay * 2, _mqttOptions.Value.ReconnectMaxDelayMs);
        }
    }

    private async Task SubscribeAsync(CancellationToken ct)
    {
        if (_mqttClient is null) return;
        var sub = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic("climate-hub/v1/+/+/telemetry/environment").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f.WithTopic("climate-hub/v1/+/+/command/ack").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f.WithTopic("climate-hub/v1/+/+/command/progress").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f.WithTopic("climate-hub/v1/+/+/command/result").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f.WithTopic("climate-hub/v1/+/+/command/error").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build();
        var result = await _mqttClient.SubscribeAsync(sub, ct);
        _logger.LogInformation("Subscribed to telemetry and command topics");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Device Gateway stopping");
        _telemetryChannel.Writer.TryComplete();
        _commandChannel.Writer.TryComplete();
        if (_mqttClient?.IsConnected == true) await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
        _mqttClient?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}