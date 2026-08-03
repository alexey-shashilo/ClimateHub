using System.Net.Http.Json;
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

namespace ClimateHub.DeploymentVerificationTests;

public class FullStackFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _postgres;
    private readonly IContainer _mqtt;
    private readonly IContainer _influx;

    public string PostgresConnectionString { get; private set; } = "";
    public int MqttPort { get; private set; }
    public string MqttHost { get; private set; } = "localhost";
    public string InfluxDbUrl { get; private set; } = "";
    public string InfluxDbToken { get; private set; } = "deploy-test-token-deploy";
    public HttpClient ApiClient { get; private set; } = null!;
    public IMqttClient AdminMqtt { get; private set; } = null!;
    public string AuthToken { get; private set; } = "";
    public IHost GatewayHost { get; private set; } = null!;

    public FullStackFixture()
    {
        _postgres = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_deploy")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_deploy")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        _mqtt = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20")
            .WithPortBinding(1883, true)
            .WithResourceMapping(Encoding.UTF8.GetBytes("listener 1883\nprotocol mqtt\nallow_anonymous true\npersistence false\nlog_dest stdout"), "/mosquitto/config/mosquitto.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .Build();

        _influx = new ContainerBuilder()
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
        await _postgres.StartAsync();
        await _mqtt.StartAsync();
        await _influx.StartAsync();

        PostgresConnectionString = $"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Database=climate_hub_deploy;Username=climate_hub;Password=climate_hub_deploy;";
        MqttPort = _mqtt.GetMappedPublicPort(1883);
        InfluxDbUrl = $"http://{_influx.Hostname}:{_influx.GetMappedPublicPort(8086)}";

        AuthToken = GenerateAuthToken();
        ApiClient = CreateAuthenticatedClient();

        AdminMqtt = await ConnectMqttAsync();

        var gwBuilder = Host.CreateApplicationBuilder();
        gwBuilder.Environment.EnvironmentName = "Test";
        gwBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = PostgresConnectionString,
            ["Mqtt:Host"] = MqttHost, ["Mqtt:Port"] = MqttPort.ToString(), ["Mqtt:ClientId"] = "climate-hub-deploy-gateway",
            ["Mqtt:ReconnectBaseDelayMs"] = "1000", ["Mqtt:ReconnectMaxDelayMs"] = "5000",
            ["InfluxDb:Url"] = InfluxDbUrl, ["InfluxDb:Token"] = InfluxDbToken, ["InfluxDb:Organization"] = "climate-hub", ["InfluxDb:Bucket"] = "climate-hub",
        });

        gwBuilder.Services.AddClimateHubInfrastructure();
        gwBuilder.Services.AddBuildingModule(PostgresConnectionString);
        gwBuilder.Services.AddDevicesModule(PostgresConnectionString);
        gwBuilder.Services.AddEnvironmentModule(PostgresConnectionString);
        gwBuilder.Services.AddCommandsModule(PostgresConnectionString);
        gwBuilder.Services.AddNeedsModule(PostgresConnectionString);
        gwBuilder.Services.AddClimateModule(PostgresConnectionString);
        gwBuilder.Services.AddEngineeringSystemsModule(PostgresConnectionString);
        gwBuilder.Services.AddScoped<ClimateHub.DeviceGateway.Services.TelemetryIngestionHandler>();
        gwBuilder.Services.AddScoped<ClimateHub.DeviceGateway.Services.CommandEventConsumer>();
        gwBuilder.Services.AddHostedService<ClimateHub.DeviceGateway.DeviceGatewayWorker>();

        GatewayHost = gwBuilder.Build();
        await GatewayHost.StartAsync();
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AuthToken);
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
                ["Mqtt:Host"] = MqttHost, ["Mqtt:Port"] = MqttPort.ToString(), ["Mqtt:ClientId"] = "climate-hub-deploy-api",
                ["InfluxDb:Url"] = InfluxDbUrl, ["InfluxDb:Token"] = InfluxDbToken, ["InfluxDb:Organization"] = "climate-hub", ["InfluxDb:Bucket"] = "climate-hub",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                ["Jwt:Issuer"] = "ClimateHub", ["Jwt:Audience"] = "ClimateHub.Api",
            });
        });
    }

    private async Task<IMqttClient> ConnectMqttAsync()
    {
        var factory = new MqttClientFactory();
        var client = factory.CreateMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(MqttHost, MqttPort).WithClientId($"deploy-admin-{Guid.NewGuid():N}"[..20]).WithCleanSession().Build());
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        try { await GatewayHost.StopAsync(TimeSpan.FromSeconds(10)); GatewayHost.Dispose(); } catch { }
        if (AdminMqtt?.IsConnected == true) await AdminMqtt.DisconnectAsync();
        AdminMqtt?.Dispose(); ApiClient?.Dispose();
        await _influx.DisposeAsync(); await _mqtt.DisposeAsync(); await _postgres.DisposeAsync();
    }

    public override async ValueTask DisposeAsync() { await ((IAsyncLifetime)this).DisposeAsync(); }

    private static string GenerateAuthToken()
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-signing-key-that-is-at-least-32-characters-long"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "ClimateHub", audience: "ClimateHub.Api",
            claims: [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "deploy-test")],
            expires: DateTime.UtcNow.AddHours(2), signingCredentials: creds);
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}

[Collection("Docker")]
public class DeploymentVerificationTests : IClassFixture<FullStackFixture>
{
    private readonly FullStackFixture _fixture;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public DeploymentVerificationTests(FullStackFixture fixture)
    {
        _fixture = fixture;
        _http = fixture.ApiClient;
    }

    [Fact]
    public async Task Health_LivenessEndpoint_Returns200()
    {
        var resp = await _http.GetAsync("/health/live");
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Health_ReadinessEndpoint_Returns200()
    {
        var resp = await _http.GetAsync("/health/ready");
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task JwtAuth_RejectsUnauthenticated()
    {
        using var unauth = _fixture.CreateClient();
        var resp = await unauth.GetAsync("/api/v1/buildings");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Building_CreateAndRead_Works()
    {
        var createResp = await _http.PostAsJsonAsync("/api/v1/buildings", new { name = "DeployTest" });
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>(Json);
        var id = created.GetProperty("id").GetString();
        Assert.NotNull(id);

        var getResp = await _http.GetAsync($"/api/v1/buildings/{id}");
        getResp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Device_RegisterAndAssign_Works()
    {
        var building = await CreateBuilding();
        var floor = await CreateFloor(building);
        var room = await CreateRoom(floor);

        var dev = await _http.PostAndReadAsync("/api/v1/devices", new
        {
            hardwareId = $"DEPLOY-DEV-{Guid.NewGuid():N}"[..20], name = "Deploy Device",
            manufacturer = "Test", modelName = "T-100", protocolVersion = "1.0"
        });
        var deviceId = dev.GetProperty("id").GetString();
        Assert.NotNull(deviceId);

        var assignResp = await _http.PostAsJsonAsync($"/api/v1/devices/{deviceId}/assignments", new { roomId = room });
        assignResp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Mqtt_PublishSubscribe_RealBroker()
    {
        var factory = new MqttClientFactory();
        var pub = factory.CreateMqttClient();
        var sub = factory.CreateMqttClient();

        await pub.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(_fixture.MqttHost, _fixture.MqttPort).WithClientId("deploy-pub").WithCleanSession().Build());
        await sub.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(_fixture.MqttHost, _fixture.MqttPort).WithClientId("deploy-sub").WithCleanSession().Build());

        var tcs = new TaskCompletionSource<string>();
        sub.ApplicationMessageReceivedAsync += e =>
        {
            tcs.TrySetResult(Encoding.UTF8.GetString(e.ApplicationMessage.Payload.FirstSpan));
            return Task.CompletedTask;
        };

        await sub.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("deploy/test", MqttQualityOfServiceLevel.AtLeastOnce).Build());
        await pub.PublishAsync(new MqttApplicationMessageBuilder().WithTopic("deploy/test").WithPayload("deploy-ok").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());

        var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("deploy-ok", result);

        await sub.DisconnectAsync(); await pub.DisconnectAsync();
        sub.Dispose(); pub.Dispose();
    }

    [Fact]
    public async Task TelemetryPipeline_EndToEnd()
    {
        var building = await CreateBuilding();
        var floor = await CreateFloor(building);
        var room = await CreateRoom(floor);

        var sensor = await _http.PostAndReadAsync("/api/v1/devices", new
        {
            hardwareId = $"DEPLOY-SENSOR-{Guid.NewGuid():N}"[..20], name = "Deploy Sensor",
            manufacturer = "Test", modelName = "S-100", protocolVersion = "1.0"
        });
        var sensorId = sensor.GetProperty("id").GetString()!;
        await _http.PostAsJsonAsync($"/api/v1/devices/{sensorId}/assignments", new { roomId = room });

        var telemetry = System.Text.Json.JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(), messageType = "environment.telemetry", protocolVersion = "1.0",
            buildingId = building, deviceId = sensorId, bootId = Guid.NewGuid().ToString(),
            sequenceNumber = 1, measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { temperatureC = 23.5, relativeHumidityPct = 45.0 }
        });

        var msg = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{building}/{sensorId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry)).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var pubResult = await _fixture.AdminMqtt.PublishAsync(msg);
        Assert.True(pubResult.IsSuccess);

        await Task.Delay(3000);

        var envResp = await _http.GetAsync($"/api/v1/rooms/{room}/environment");
        envResp.EnsureSuccessStatusCode();
    }

    private async Task<string> CreateBuilding()
    {
        var resp = await _http.PostAndReadAsync("/api/v1/buildings", new { name = $"Deploy-{Guid.NewGuid():N}"[..15] });
        return resp.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateFloor(string buildingId)
    {
        var resp = await _http.PostAndReadAsync($"/api/v1/buildings/{buildingId}/floors", new { name = "Ground", level = 0 });
        return resp.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateRoom(string floorId)
    {
        var resp = await _http.PostAndReadAsync($"/api/v1/floors/{floorId}/rooms", new { name = "Deploy Room" });
        return resp.GetProperty("id").GetString()!;
    }
}

internal static class HttpExt
{
    public static async Task<JsonElement> PostAndReadAsync(this HttpClient http, string url, object body)
    {
        var resp = await http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }
}