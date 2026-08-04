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
    public string PostgresConnectionString { get; private set; } = "Host=localhost;Port=5432;Database=climate_hub_e2e;Username=climate_hub;Password=climate_hub_e2e;";
    public int MqttPort { get; private set; } = 1883;
    public string MqttHost { get; private set; } = "localhost";
    public string InfluxDbUrl { get; private set; } = "http://localhost:8086";
    public string InfluxDbToken { get; private set; } = "e2e-test-token-e2e";
    public IMqttClient AdminMqttClient { get; private set; } = null!;
    public HttpClient ApiClient { get; private set; } = null!;
    public IHost GatewayHost { get; private set; } = null!;

    public E2eProductionFixture()
    {
        PostgresConnectionString = Environment.GetEnvironmentVariable("E2E_PG_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=climate_hub_e2e;Username=climate_hub;Password=climate_hub_e2e;";
        MqttPort = int.TryParse(Environment.GetEnvironmentVariable("E2E_MQTT_PORT"), out var mp) ? mp : 1883;
        var influxPort = int.TryParse(Environment.GetEnvironmentVariable("E2E_INFLUX_PORT"), out var ip) ? ip : 8086;
        InfluxDbUrl = $"http://localhost:{influxPort}";
    }

    public async Task InitializeAsync()
    {
        ApiClient = CreateAuthenticatedClient();
        AdminMqttClient = await ConnectAdminMqttClient();
        await StartGatewayAsync();
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = TestAuthHelper.GenerateToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-that-is-at-least-32-characters-long");
        builder.UseSetting("Jwt:Issuer", "ClimateHub");
        builder.UseSetting("Jwt:Audience", "ClimateHub.Api");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:ConnectionString"] = PostgresConnectionString,
                ["Mqtt:Host"] = MqttHost, ["Mqtt:Port"] = MqttPort.ToString(), ["Mqtt:ClientId"] = "climate-hub-test-api",
                ["InfluxDb:Url"] = InfluxDbUrl, ["InfluxDb:Token"] = InfluxDbToken,
                ["InfluxDb:Organization"] = "climate-hub", ["InfluxDb:Bucket"] = "climate-hub",
            });
        });
    }

    public async Task StartGatewayAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = "Test";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = PostgresConnectionString,
            ["Mqtt:Host"] = MqttHost, ["Mqtt:Port"] = MqttPort.ToString(), ["Mqtt:ClientId"] = "climate-hub-test-gateway",
            ["Mqtt:ReconnectBaseDelayMs"] = "1000", ["Mqtt:ReconnectMaxDelayMs"] = "5000",
            ["InfluxDb:Url"] = InfluxDbUrl, ["InfluxDb:Token"] = InfluxDbToken,
            ["InfluxDb:Organization"] = "climate-hub", ["InfluxDb:Bucket"] = "climate-hub",
        });

        builder.Services.AddClimateHubInfrastructure();
        builder.Services.AddBuildingModule(PostgresConnectionString);
        builder.Services.AddDevicesModule(PostgresConnectionString);
        builder.Services.AddEnvironmentModule(PostgresConnectionString);
        builder.Services.AddCommandsModule(PostgresConnectionString);
        builder.Services.AddNeedsModule(PostgresConnectionString);
        builder.Services.AddClimateModule(PostgresConnectionString);
        builder.Services.AddEngineeringSystemsModule(PostgresConnectionString);
        builder.Services.AddScoped<ClimateHub.DeviceGateway.Services.TelemetryIngestionHandler>();
        builder.Services.AddScoped<ClimateHub.DeviceGateway.Services.CommandEventConsumer>();
        builder.Services.AddHostedService<ClimateHub.DeviceGateway.DeviceGatewayWorker>();

        GatewayHost = builder.Build();
        await GatewayHost.StartAsync();
    }

    public async Task StopGatewayAsync()
    {
        if (GatewayHost is not null)
        {
            await GatewayHost.StopAsync(TimeSpan.FromSeconds(10));
            GatewayHost.Dispose();
            GatewayHost = null!;
        }
    }

    public async Task PublishTelemetryAsync(string buildingId, string deviceId, object payload)
    {
        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(), messageType = "environment.telemetry", protocolVersion = "1.0",
            buildingId, deviceId, bootId = Guid.NewGuid().ToString(),
            sequenceNumber = Random.Shared.Next(1, 100000), measuredAt = DateTimeOffset.UtcNow.ToString("O"), payload
        });

        await AdminMqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
    }

    private async Task<IMqttClient> ConnectAdminMqttClient()
    {
        var factory = new MqttClientFactory();
        var client = factory.CreateMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(MqttHost, MqttPort).WithClientId($"e2e-admin-{Guid.NewGuid():N}"[..20]).WithCleanSession().Build());
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await StopGatewayAsync();
        if (AdminMqttClient?.IsConnected == true) await AdminMqttClient.DisconnectAsync();
        AdminMqttClient?.Dispose(); ApiClient?.Dispose();
    }

    public override async ValueTask DisposeAsync() { await ((IAsyncLifetime)this).DisposeAsync(); }
}