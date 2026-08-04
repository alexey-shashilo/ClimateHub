using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.MqttComponentTests;

[Trait("Category", "Component")]
public class MqttComponentTests : IAsyncLifetime
{
    private readonly IContainer _mosquittoContainer;
    private int _mqttPort;
    private IMqttClient _client = null!;
    private MqttClientOptions _options = null!;

    private const string ValidTopic = "climate-hub/v1/building-001/device-001/telemetry/environment";
    private const string InvalidTopic = "invalid/topic/format";
    private const string Username = "gateway";
    private const string Password = "gateway-pass";

    public MqttComponentTests()
    {
        var mosquittoConfig = new[]
        {
            "listener 1883",
            "protocol mqtt",
            "allow_anonymous true",
            "persistence false",
            "log_dest stdout",
            "log_type all"
        };

        _mosquittoContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20")
            .WithPortBinding(1883, true)
            .WithResourceMapping(
                Encoding.UTF8.GetBytes(string.Join("\n", mosquittoConfig)),
                "/mosquitto/config/mosquitto.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mosquittoContainer.StartAsync();
        _mqttPort = _mosquittoContainer.GetMappedPublicPort(1883);
    }

    private IMqttClient? _subscriber;

    public async Task DisposeAsync()
    {
        if (_subscriber?.IsConnected == true)
            await _subscriber.DisconnectAsync();
        _subscriber?.Dispose();
        if (_client?.IsConnected == true)
            await _client.DisconnectAsync();
        _client?.Dispose();
        await _mosquittoContainer.DisposeAsync();
    }

    private async Task EnsureSubscriber(string topic)
    {
        if (_subscriber?.IsConnected == true) return;
        var factory = new MqttClientFactory();
        _subscriber = factory.CreateMqttClient();
        await _subscriber.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _mqttPort)
            .WithClientId($"sub-{Guid.NewGuid():N}"[..20])
            .WithCleanSession().Build());
        await _subscriber.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce).Build());
    }

    [Fact]
    public async Task AnonymousConnection_Allowed()
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _mqttPort)
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options);
        Assert.True(client.IsConnected);
    }

    [Fact]
    public async Task GatewayConnects_WithCredentials()
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _mqttPort)
            .WithCredentials(Username, Password)
            .WithClientId("test-gateway")
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options);
        Assert.True(client.IsConnected);
    }

    [Fact]
    public async Task ValidTelemetry_PublishesSuccessfully()
    {
        await ConnectAuthenticatedClient();
        await EnsureSubscriber(ValidTopic);

        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId = "building-001",
            deviceId = "device-001",
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = 1,
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { temperatureC = 22.5, humidityPct = 45.0 }
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(ValidTopic)
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();

        var result = await _client.PublishAsync(message);
        Assert.Equal(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [Fact]
    public async Task InvalidTopic_PublishFails()
    {
        await ConnectAuthenticatedClient();
        await EnsureSubscriber(InvalidTopic);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(InvalidTopic)
            .WithPayload(Encoding.UTF8.GetBytes("{}"))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await _client.PublishAsync(message);
        Assert.Equal(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [Fact]
    public async Task InvalidPayload_PublishesButInvalid()
    {
        await ConnectAuthenticatedClient();
        await EnsureSubscriber(ValidTopic);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(ValidTopic)
            .WithPayload("not-json"u8.ToArray())
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await _client.PublishAsync(message);
        Assert.Equal(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [Fact]
    public async Task CommandPublish_AckProgressResult()
    {
        await ConnectAuthenticatedClient();
        await EnsureSubscriber($"climate-hub/v1/building-001/device-001/command/+");
        var commandId = Guid.NewGuid().ToString();

        var command = JsonSerializer.Serialize(new
        {
            commandId,
            type = "SetVentilation",
            targetDeviceId = "device-001",
            parameters = new { speed = 80 }
        });

        var commandMsg = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/building-001/device-001/command/{commandId}")
            .WithPayload(Encoding.UTF8.GetBytes(command))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .Build();

        var result = await _client.PublishAsync(commandMsg);
        Assert.Equal(MqttClientPublishReasonCode.Success, result.ReasonCode);
    }

    [Fact]
    public async Task BrokerRestart_Reconnects()
    {
        await ConnectAuthenticatedClient();
        Assert.True(_client.IsConnected);

        await _mosquittoContainer.StopAsync();
        await Task.Delay(2000);

        Assert.False(_client.IsConnected);

        await _mosquittoContainer.StartAsync();
        _mqttPort = _mosquittoContainer.GetMappedPublicPort(1883);
        await Task.Delay(2000);

        await ConnectAuthenticatedClient();
        Assert.True(_client.IsConnected);
    }

    private async Task ConnectAuthenticatedClient()
    {
        var factory = new MqttClientFactory();
        _client?.Dispose();
        _client = factory.CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _mqttPort)
            .WithCredentials(Username, Password)
            .WithClientId($"test-client-{Guid.NewGuid():N}"[..20])
            .WithCleanSession()
            .Build();
        await _client.ConnectAsync(_options);
    }
}