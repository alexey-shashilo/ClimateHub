using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.DeploymentVerificationTests;

[Collection("Docker")]
public class DeploymentVerificationTests : IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private readonly IContainer _mqttContainer;
    private readonly IContainer _influxDbContainer;
    private string _postgresConnectionString = string.Empty;
    private int _mqttPort;
    private string _influxDbUrl = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DeploymentVerificationTests()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_deploy")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_deploy")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        var mosquittoConfig = new[]
        {
            "listener 1883",
            "protocol mqtt",
            "allow_anonymous true",
            "persistence false",
            "log_dest stdout"
        };

        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20")
            .WithPortBinding(1883, true)
            .WithResourceMapping(Encoding.UTF8.GetBytes(string.Join("\n", mosquittoConfig)), "/mosquitto/config/mosquitto.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();

        _influxDbContainer = new ContainerBuilder()
            .WithImage("influxdb:2.7.11-alpine")
            .WithPortBinding(8086, true)
            .WithEnvironment("DOCKER_INFLUXDB_INIT_MODE", "setup")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_USERNAME", "admin")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_PASSWORD", "admin123456")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_ORG", "climate-hub")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_BUCKET", "climate-hub")
            .WithEnvironment("DOCKER_INFLUXDB_INIT_ADMIN_TOKEN", "deploy-test-token")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8086))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _mqttContainer.StartAsync();
        await _influxDbContainer.StartAsync();

        _postgresConnectionString =
            $"Host={_postgresContainer.Hostname};Port={_postgresContainer.GetMappedPublicPort(5432)};" +
            $"Database=climate_hub_deploy;Username=climate_hub;Password=climate_hub_deploy;";

        _mqttPort = _mqttContainer.GetMappedPublicPort(1883);
        _influxDbUrl = $"http://{_influxDbContainer.Hostname}:{_influxDbContainer.GetMappedPublicPort(8086)}";
    }

    public async Task DisposeAsync()
    {
        await _influxDbContainer.DisposeAsync();
        await _mqttContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Postgres_IsRunningAndAcceptsConnections()
    {
        using var conn = new Npgsql.NpgsqlConnection(_postgresConnectionString);
        await conn.OpenAsync();
        Assert.Equal(System.Data.ConnectionState.Open, conn.State);
    }

    [Fact]
    public async Task Mqtt_IsRunningAndAcceptsConnections()
    {
        using var mqttClient = new System.Net.Sockets.TcpClient();
        await mqttClient.ConnectAsync("localhost", _mqttPort);
        Assert.True(mqttClient.Connected);
        mqttClient.Close();
    }

    [Fact]
    public async Task InfluxDb_IsRunningAndRespondsToPing()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await httpClient.GetAsync($"{_influxDbUrl}/ping");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Mqtt_PublishAndSubscribe()
    {
        var mqttFactory = new MqttClientFactory();
        var publisher = mqttFactory.CreateMqttClient();
        var subscriber = mqttFactory.CreateMqttClient();

        var pubOpts = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _mqttPort).WithClientId("deploy-pub").WithCleanSession().Build();
        var subOpts = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _mqttPort).WithClientId("deploy-sub").WithCleanSession().Build();

        await publisher.ConnectAsync(pubOpts);
        await subscriber.ConnectAsync(subOpts);

        var tcs = new TaskCompletionSource<string>();
        subscriber.ApplicationMessageReceivedAsync += e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.FirstSpan);
            tcs.TrySetResult(payload);
            return Task.CompletedTask;
        };

        await subscriber.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter("deploy/test", MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        await publisher.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic("deploy/test").WithPayload("deploy-verification-ok")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build());

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("deploy-verification-ok", result);

        await subscriber.DisconnectAsync();
        await publisher.DisconnectAsync();
        subscriber.Dispose();
        publisher.Dispose();
    }
}