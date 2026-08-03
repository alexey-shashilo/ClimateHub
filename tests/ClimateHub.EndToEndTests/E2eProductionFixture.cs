using System.Text;
using System.Text.Json;
using ClimateHub.Infrastructure.Configuration;
using ClimateHub.Modules.Building.Infrastructure;
using ClimateHub.Modules.Commands.Infrastructure;
using ClimateHub.Modules.Devices.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure;
using ClimateHub.Modules.Needs;
using ClimateHub.Modules.Needs.Infrastructure;
using ClimateHub.Modules.Climate;
using ClimateHub.Modules.Climate.Infrastructure;
using ClimateHub.Modules.EngineeringSystems;
using ClimateHub.Modules.EngineeringSystems.Infrastructure;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.EndToEndTests;

public class E2eProductionFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private readonly IContainer _mqttContainer;
    private readonly IContainer _influxDbContainer;

    public string PostgresConnectionString { get; private set; } = string.Empty;
    public int MqttPort { get; private set; }
    public string MqttHost { get; private set; } = "localhost";
    public string InfluxDbUrl { get; private set; } = string.Empty;
    public string InfluxDbToken { get; private set; } = "e2e-test-token-e2e";
    public IMqttClient AdminMqttClient { get; private set; } = null!;
    public HttpClient ApiClient { get; private set; } = null!;
    public string AuthToken { get; private set; } = string.Empty;

    private DeviceGatewayHost _gatewayHost = null!;

    public E2eProductionFixture()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_e2e")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_e2e")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        var mosquittoConfig = new[]
        {
            "listener 1883",
            "protocol mqtt",
            "allow_anonymous true",
            "persistence false",
            "log_dest stdout",
            "log_type all",
            "connection_messages true"
        };

        _mqttContainer = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20")
            .WithPortBinding(1883, true)
            .WithResourceMapping(
                Encoding.UTF8.GetBytes(string.Join("\n", mosquittoConfig)),
                "/mosquitto/config/mosquitto.conf")
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
            .WithEnvironment("DOCKER_INFLUXDB_INIT_ADMIN_TOKEN", InfluxDbToken)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8086))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _mqttContainer.StartAsync();
        await _influxDbContainer.StartAsync();

        PostgresConnectionString =
            $"Host={_postgresContainer.Hostname};" +
            $"Port={_postgresContainer.GetMappedPublicPort(5432)};" +
            $"Database=climate_hub_e2e;Username=climate_hub;Password=climate_hub_e2e;";

        MqttPort = _mqttContainer.GetMappedPublicPort(1883);
        MqttHost = _mqttContainer.Hostname ?? "localhost";
        InfluxDbUrl = $"http://{_influxDbContainer.Hostname}:{_influxDbContainer.GetMappedPublicPort(8086)}";

        AuthToken = TestAuthHelper.GenerateToken();
        ApiClient = CreateAuthenticatedClient();

        _gatewayHost = new DeviceGatewayHost(MqttHost, MqttPort, PostgresConnectionString, InfluxDbUrl, InfluxDbToken);
        await _gatewayHost.StartAsync();

        AdminMqttClient = await ConnectAdminMqttClient();
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = PostgresConnectionString,
                ["Mqtt:Host"] = MqttHost,
                ["Mqtt:Port"] = MqttPort.ToString(),
                ["Mqtt:ClientId"] = "climate-hub-test-api",
                ["InfluxDb:Url"] = InfluxDbUrl,
                ["InfluxDb:Token"] = InfluxDbToken,
                ["InfluxDb:Organization"] = "climate-hub",
                ["InfluxDb:Bucket"] = "climate-hub",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                ["Jwt:Issuer"] = "ClimateHub",
                ["Jwt:Audience"] = "ClimateHub.Api",
            });
        });
    }

    private async Task<IMqttClient> ConnectAdminMqttClient()
    {
        var factory = new MqttClientFactory();
        var client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(MqttHost, MqttPort)
            .WithClientId($"e2e-admin-{Guid.NewGuid():N}"[..20])
            .WithCleanSession()
            .Build();
        await client.ConnectAsync(options);
        return client;
    }

    public async Task PublishTelemetryAsync(string buildingId, string deviceId, object payload)
    {
        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId,
            deviceId,
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = Random.Shared.Next(1, 100000),
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await AdminMqttClient.PublishAsync(message);
    }

    public async Task StopGatewayAsync()
    {
        await _gatewayHost.StopAsync();
    }

    public async Task StartGatewayAsync()
    {
        _gatewayHost = new DeviceGatewayHost(MqttHost, MqttPort, PostgresConnectionString, InfluxDbUrl, InfluxDbToken);
        await _gatewayHost.StartAsync();
    }

    public async Task PublishCommandEventAsync(string buildingId, string deviceId, string eventType, object payload)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = $"command.{eventType}",
            protocolVersion = "1.0",
            buildingId,
            deviceId,
            commandId = payload is JsonElement je ? je.GetProperty("commandId").GetString() : "",
            attemptNumber = 1,
            createdAt = DateTimeOffset.UtcNow.ToString("O"),
            payload
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/command/{eventType}")
            .WithPayload(Encoding.UTF8.GetBytes(envelope))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await AdminMqttClient.PublishAsync(message);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _gatewayHost.StopAsync();
        if (AdminMqttClient?.IsConnected == true)
            await AdminMqttClient.DisconnectAsync();
        AdminMqttClient?.Dispose();
        ApiClient?.Dispose();
        await _influxDbContainer.DisposeAsync();
        await _mqttContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await ((IAsyncLifetime)this).DisposeAsync();
    }
}

public class DeviceGatewayHost
{
    private readonly string _mqttHost;
    private readonly int _mqttPort;
    private readonly string _postgresConnectionString;
    private readonly string _influxDbUrl;
    private readonly string _influxDbToken;
    private Task _gatewayTask = Task.CompletedTask;
    private CancellationTokenSource _cts = null!;

    public DeviceGatewayHost(string mqttHost, int mqttPort, string postgresConnectionString,
        string influxDbUrl, string influxDbToken)
    {
        _mqttHost = mqttHost;
        _mqttPort = mqttPort;
        _postgresConnectionString = postgresConnectionString;
        _influxDbUrl = influxDbUrl;
        _influxDbToken = influxDbToken;
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        _gatewayTask = Task.Run(() => RunGatewayAsync(_cts.Token));
        await Task.Delay(5000);
    }

    private async Task RunGatewayAsync(CancellationToken ct)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = "Test";

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = _postgresConnectionString,
            ["Mqtt:Host"] = _mqttHost,
            ["Mqtt:Port"] = _mqttPort.ToString(),
            ["Mqtt:ClientId"] = "climate-hub-test-gateway",
            ["Mqtt:ReconnectBaseDelayMs"] = "1000",
            ["Mqtt:ReconnectMaxDelayMs"] = "5000",
            ["InfluxDb:Url"] = _influxDbUrl,
            ["InfluxDb:Token"] = _influxDbToken,
            ["InfluxDb:Organization"] = "climate-hub",
            ["InfluxDb:Bucket"] = "climate-hub",
            ["CommandTimeout:ExecutionTimeout"] = "00:01:00",
            ["CommandTimeout:QueuedTimeout"] = "00:00:30",
            ["CommandTimeout:ScanInterval"] = "00:00:15",
            ["CommandTimeout:InitialDelayMs"] = "3000",
        });

        builder.Services.AddClimateHubInfrastructure();
        builder.Services.AddBuildingModule(_postgresConnectionString);
        builder.Services.AddDevicesModule(_postgresConnectionString);
        builder.Services.AddEnvironmentModule(_postgresConnectionString);
        builder.Services.AddCommandsModule(_postgresConnectionString);
        builder.Services.AddNeedsModule(_postgresConnectionString);
        builder.Services.AddClimateModule(_postgresConnectionString);
        builder.Services.AddEngineeringSystemsModule(_postgresConnectionString);
        builder.Services.AddScoped<ClimateHub.DeviceGateway.Services.TelemetryIngestionHandler>();
        builder.Services.AddScoped<ClimateHub.DeviceGateway.Services.CommandEventConsumer>();
        builder.Services.AddHostedService<ClimateHub.DeviceGateway.DeviceGatewayWorker>();

        var host = builder.Build();
        await host.RunAsync(ct);
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        try { await _gatewayTask; } catch (OperationCanceledException) { }
    }
}