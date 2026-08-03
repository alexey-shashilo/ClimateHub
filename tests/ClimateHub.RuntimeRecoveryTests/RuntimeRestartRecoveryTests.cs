using System.Net.Http.Json;
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

namespace ClimateHub.RuntimeRecoveryTests;

[Collection("Docker")]
public class RuntimeRestartRecoveryTests : IAsyncLifetime
{
    private readonly IContainer _postgres;
    private readonly IContainer _mqtt;
    private string _connectionString = "";
    private int _mqttPort;
    private string _mqttHost = "localhost";

    public RuntimeRestartRecoveryTests()
    {
        _postgres = new ContainerBuilder()
            .WithImage("postgres:17-alpine").WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_recovery")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_recovery")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432)).Build();

        _mqtt = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2.0.20").WithPortBinding(1883, true)
            .WithResourceMapping(
                System.Text.Encoding.UTF8.GetBytes("listener 1883\nprotocol mqtt\nallow_anonymous true\npersistence false\nlog_dest stdout"),
                "/mosquitto/config/mosquitto.conf")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883)).Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _mqtt.StartAsync();
        _connectionString = $"Host={_postgres.Hostname};Port={_postgres.GetMappedPublicPort(5432)};Database=climate_hub_recovery;Username=climate_hub;Password=climate_hub_recovery;";
        _mqttPort = _mqtt.GetMappedPublicPort(1883);
    }

    public async Task DisposeAsync()
    {
        await _mqtt.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private HttpClient CreateRecoveryClient(string clientId)
    {
        var opts = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var factory = new RecoveryApiHost
        {
            ConnectionString = _connectionString,
            MqttHost = _mqttHost,
            MqttPort = _mqttPort,
            MqttClientId = clientId
        };
        var client = factory.CreateClient(opts);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken());
        return client;
    }

    [Fact]
    public async Task BuildingAndDevice_ExistAfterRestart()
    {
        string buildingId;
        string deviceId;

        using (var client1 = CreateRecoveryClient("recovery-host-1"))
        {
            var building = await client1.PostReadAsync("/api/v1/buildings", new { name = "Recovery Building" });
            buildingId = building.GetProperty("id").GetString()!;

            var floor = await client1.PostReadAsync($"/api/v1/buildings/{buildingId}/floors", new { name = "Ground", level = 0 });
            var floorId = floor.GetProperty("id").GetString()!;

            await client1.PostReadAsync($"/api/v1/floors/{floorId}/rooms", new { name = "Recovery Room" });

            var dev = await client1.PostReadAsync("/api/v1/devices", new
            {
                hardwareId = $"RECOVERY-{Guid.NewGuid():N}"[..20], name = "Recovery Sensor",
                manufacturer = "Test", modelName = "R-100", protocolVersion = "1.0"
            });
            deviceId = dev.GetProperty("id").GetString()!;
        }

        using (var client2 = CreateRecoveryClient("recovery-host-2"))
        {
            var buildingResp = await client2.GetAsync($"/api/v1/buildings/{buildingId}");
            Assert.True(buildingResp.IsSuccessStatusCode, "Building should exist after restart");

            var deviceResp = await client2.GetAsync($"/api/v1/devices/{deviceId}");
            Assert.True(deviceResp.IsSuccessStatusCode, "Device should exist after restart");

            var live = await client2.GetAsync("/health/live");
            Assert.True(live.IsSuccessStatusCode, "Liveness should pass after restart");

            var ready = await client2.GetAsync("/health/ready");
            Assert.True(ready.IsSuccessStatusCode, "Readiness should pass after restart");
        }
    }

    [Fact]
    public async Task WorkersResume_AfterRestart()
    {
        using (var client1 = CreateRecoveryClient("recovery-workers-1"))
        {
            await client1.GetAsync("/api/v1/buildings");
        }

        using (var client2 = CreateRecoveryClient("recovery-workers-2"))
        {
            var live = await client2.GetAsync("/health/live");
            Assert.True(live.IsSuccessStatusCode);

            var ready = await client2.GetAsync("/health/ready");
            Assert.True(ready.IsSuccessStatusCode);

            var buildings = await client2.GetAsync("/api/v1/buildings");
            Assert.True(buildings.IsSuccessStatusCode);
        }
    }

    [Fact]
    public async Task HealthEndpoints_WorkAfterRestart()
    {
        using (var client1 = CreateRecoveryClient("recovery-health-1"))
        {
            var live1 = await client1.GetAsync("/health/live");
            Assert.True(live1.IsSuccessStatusCode);

            var ready1 = await client1.GetAsync("/health/ready");
            Assert.True(ready1.IsSuccessStatusCode);

            var building = await client1.PostReadAsync("/api/v1/buildings", new { name = "Health Test" });
            Assert.NotNull(building.GetProperty("id").GetString());
        }

        using (var client2 = CreateRecoveryClient("recovery-health-2"))
        {
            var live2 = await client2.GetAsync("/health/live");
            Assert.True(live2.IsSuccessStatusCode);

            var ready2 = await client2.GetAsync("/health/ready");
            Assert.True(ready2.IsSuccessStatusCode);
        }
    }

    private static string GenerateToken()
    {
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("test-signing-key-that-is-at-least-32-characters-long"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "ClimateHub", audience: "ClimateHub.Api",
            claims: new[] { new System.Security.Claims.Claim("permission", "building_read"),
                           new System.Security.Claims.Claim("permission", "building_configure"),
                           new System.Security.Claims.Claim("permission", "device_read"),
                           new System.Security.Claims.Claim("permission", "device_configure"),
                           new System.Security.Claims.Claim("permission", "environment_read") },
            expires: DateTime.UtcNow.AddHours(2), signingCredentials: creds);
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private class RecoveryApiHost : WebApplicationFactory<Program>
    {
        public string ConnectionString { get; set; } = "";
        public int MqttPort { get; set; }
        public string MqttHost { get; set; } = "localhost";
        public string MqttClientId { get; set; } = "climate-hub-recovery";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Postgres:ConnectionString"] = ConnectionString,
                    ["Mqtt:Host"] = MqttHost, ["Mqtt:Port"] = MqttPort.ToString(), ["Mqtt:ClientId"] = MqttClientId,
                    ["Mqtt:ReconnectBaseDelayMs"] = "1000", ["Mqtt:ReconnectMaxDelayMs"] = "5000",
                    ["InfluxDb:Url"] = "http://localhost:8086", ["InfluxDb:Token"] = "recovery-token",
                    ["InfluxDb:Organization"] = "climate-hub", ["InfluxDb:Bucket"] = "climate-hub",
                    ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                    ["Jwt:Issuer"] = "ClimateHub", ["Jwt:Audience"] = "ClimateHub.Api",
                });
            });
        }
    }
}

internal static class HttpHelpers
{
    public static async Task<JsonElement> PostReadAsync(this HttpClient http, string url, object body)
    {
        var resp = await http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>();
    }
}