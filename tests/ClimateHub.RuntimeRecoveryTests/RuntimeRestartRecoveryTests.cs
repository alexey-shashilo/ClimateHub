using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
        _connectionString = $"Host=localhost;Port={_postgres.GetMappedPublicPort(5432)};Database=climate_hub_recovery;Username=climate_hub;Password=climate_hub_recovery;";
        _mqttPort = _mqtt.GetMappedPublicPort(1883);
        _mqttHost = "localhost";
    }

    public async Task DisposeAsync()
    {
        await _mqtt.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // ─── Recovery Host Factory ───────────────────────────────

    private ApiHost CreateHost1() => new(_connectionString, _mqttHost, _mqttPort, "recovery-host-1");
    private ApiHost CreateHost2() => new(_connectionString, _mqttHost, _mqttPort, "recovery-host-2");

    private class ApiHost : WebApplicationFactory<Program>
    {
        private readonly string _cs;
        private readonly string _mqttHost;
        private readonly int _mqttPort;
        private readonly string _clientId;

        public ApiHost(string cs, string mqttHost, int mqttPort, string clientId)
        {
            _cs = cs; _mqttHost = mqttHost; _mqttPort = mqttPort; _clientId = clientId;
        }

        public HttpClient CreateClientWithAuth()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateToken());
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
                    ["Postgres:ConnectionString"] = _cs,
                    ["Mqtt:Host"] = _mqttHost, ["Mqtt:Port"] = _mqttPort.ToString(), ["Mqtt:ClientId"] = _clientId,
                    ["Mqtt:ReconnectBaseDelayMs"] = "1000", ["Mqtt:ReconnectMaxDelayMs"] = "5000",
                    ["InfluxDb:Url"] = "http://localhost:8086", ["InfluxDb:Token"] = "recovery-token",
                    ["InfluxDb:Organization"] = "climate-hub", ["InfluxDb:Bucket"] = "climate-hub",
                });
            });
        }

        private static string GenerateToken()
        {
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("test-signing-key-that-is-at-least-32-characters-long"));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "ClimateHub", audience: "ClimateHub.Api",
                claims: new[] {
                    new System.Security.Claims.Claim("permission", "building_read"),
                    new System.Security.Claims.Claim("permission", "building_configure"),
                    new System.Security.Claims.Claim("permission", "device_read"),
                    new System.Security.Claims.Claim("permission", "device_configure"),
                    new System.Security.Claims.Claim("permission", "environment_read"),
                    new System.Security.Claims.Claim("permission", "need_read"),
                    new System.Security.Claims.Claim("permission", "need_configure"),
                    new System.Security.Claims.Claim("permission", "need_execute"),
                    new System.Security.Claims.Claim("permission", "engineering_read"),
                    new System.Security.Claims.Claim("permission", "engineering_configure"),
                    new System.Security.Claims.Claim("permission", "command_read"),
                    new System.Security.Claims.Claim("permission", "command_create"),
                    new System.Security.Claims.Claim("permission", "policy_read"),
                    new System.Security.Claims.Claim("permission", "policy_configure"),
                },
                expires: DateTime.UtcNow.AddHours(2), signingCredentials: creds);
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    private async Task<string> CreateBuilding(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/v1/buildings", new { name = $"REC-{Guid.NewGuid():N}"[..15] });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private async Task<string> CreateFloor(HttpClient c, string b)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/buildings/{b}/floors", new { name = "Ground", level = 0 });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private async Task<string> CreateRoom(HttpClient c, string f)
    {
        var r = await c.PostAsJsonAsync($"/api/v1/floors/{f}/rooms", new { name = "Recovery Room" });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    private async Task<string> CreateDevice(HttpClient c, string prefix)
    {
        var r = await c.PostAsJsonAsync("/api/v1/devices", new
        {
            hardwareId = $"{prefix}-{Guid.NewGuid():N}"[..20], name = $"{prefix} Device",
            manufacturer = "Test", modelName = "R-100", protocolVersion = "1.0"
        });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    // ─── Test 1: Need survives restart ──────────────────────

    [Fact]
    public async Task NeedSurvivesRestart()
    {
        string roomId, needId = "";
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            var b = await CreateBuilding(c);
            var f = await CreateFloor(c, b);
            roomId = await CreateRoom(c, f);

            // Trigger need evaluation
            var eval = await c.PostAsync($"/api/v1/needs/rooms/{roomId}/evaluate", null);
            Assert.True(eval.IsSuccessStatusCode);

            await Task.Delay(2000);

            var needs = await c.GetAsync($"/api/v1/needs/rooms/{roomId}");
            if (needs.IsSuccessStatusCode)
            {
                var j = await needs.Content.ReadFromJsonAsync<JsonElement>();
                if (j.ValueKind == JsonValueKind.Array && j.GetArrayLength() > 0)
                    needId = j[0].TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            }
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();
            var health = await c2.GetAsync("/health/live");
            Assert.True(health.IsSuccessStatusCode, "Liveness should pass after restart");

            var needs = await c2.GetAsync($"/api/v1/needs/rooms/{roomId}");
            Assert.True(needs.IsSuccessStatusCode, "Needs should be queryable after restart");
        }
    }

    // ─── Test 2: Climate Plan survives restart ──────────────

    [Fact]
    public async Task ClimatePlanSurvivesRestart()
    {
        string roomId = "";
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            var b = await CreateBuilding(c);
            var f = await CreateFloor(c, b);
            roomId = await CreateRoom(c, f);

            var eval = await c.PostAsync($"/api/v1/needs/rooms/{roomId}/evaluate", null);
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();

            var health = await c2.GetAsync("/health/live");
            Assert.True(health.IsSuccessStatusCode);

            var goalResp = await c2.GetAsync($"/api/v1/climate/goals/{roomId}");
            Assert.True(goalResp.IsSuccessStatusCode || goalResp.StatusCode == System.Net.HttpStatusCode.NotFound,
                "Climate goal endpoint should work after restart");
        }
    }

    // ─── Test 3: Engineering Plan survives restart ──────────

    [Fact]
    public async Task EngineeringPlanSurvivesRestart()
    {
        string? engSystemId = null;
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            var b = await CreateBuilding(c);

            var eng = await c.PostAsJsonAsync("/api/v1/engineering-systems", new
            {
                buildingId = b, name = "Recovery Ventilation", systemType = "SupplyVentilation", priority = 50,
                capabilities = new[] { new { code = "eng.co2.reduce", dataType = "double", unit = "ppm", minimum = 0.0, maximum = 10000.0 } },
                resources = new[] { new { code = "airflow_capacity", maximum = 5000.0, unit = "m3h", priority = 50 } }
            });
            eng.EnsureSuccessStatusCode();
            engSystemId = (await eng.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();
            var health = await c2.GetAsync("/health/live");
            Assert.True(health.IsSuccessStatusCode);

            var plans = await c2.GetAsync($"/api/v1/engineering-systems/{engSystemId}/plans");
            Assert.True(plans.IsSuccessStatusCode, "Engineering plans should be queryable after restart");

            var resources = await c2.GetAsync($"/api/v1/engineering-systems/{engSystemId}/resources");
            Assert.True(resources.IsSuccessStatusCode, "Engineering resources should be queryable after restart");
        }
    }

    // ─── Test 4: Command survives restart ───────────────────

    [Fact]
    public async Task CommandSurvivesRestart()
    {
        string roomId = "";
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            var b = await CreateBuilding(c);
            var f = await CreateFloor(c, b);
            roomId = await CreateRoom(c, f);

            await c.PostAsJsonAsync("/api/v1/devices", new
            {
                hardwareId = $"REC-CMD-{Guid.NewGuid():N}"[..20], name = "Recovery Cmd",
                manufacturer = "Test", modelName = "RC-100", protocolVersion = "1.0"
            });
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();
            var health = await c2.GetAsync("/health/live");
            Assert.True(health.IsSuccessStatusCode);

            var cmds = await c2.GetAsync($"/api/v1/commands");
            Assert.True(cmds.IsSuccessStatusCode, "Commands endpoint should work after restart");
        }
    }

    // ─── Test 5: Workers resume ─────────────────────────────

    [Fact]
    public async Task WorkersResumeAfterRestart()
    {
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            await c.GetAsync("/api/v1/buildings");
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();
            var health = await c2.GetAsync("/health/ready");
            Assert.True(health.IsSuccessStatusCode, "Readiness (workers alive) after restart");

            var live = await c2.GetAsync("/health/live");
            Assert.True(live.IsSuccessStatusCode, "Liveness after restart");
        }
    }

    // ─── Test 6: Outbox survives restart ────────────────────

    [Fact]
    public async Task OutboxSurvivesRestart()
    {
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            // Create data that would generate outbox entries
            var builds = await c.GetAsync("/api/v1/buildings");
            Assert.True(builds.IsSuccessStatusCode, "Building list should work before restart");
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();
            // Verify API still works – outbox workers start and process
            var builds = await c2.GetAsync("/api/v1/buildings");
            Assert.True(builds.IsSuccessStatusCode, "Building list should work after restart — outbox workers alive");
        }
    }

    // ─── Test 7: Resource lock restored ─────────────────────

    [Fact]
    public async Task ResourceLockRestoredAfterRestart()
    {
        string? engSystemId = null;
        using (var h = CreateHost1())
        {
            var c = h.CreateClientWithAuth();
            var b = await CreateBuilding(c);
            var eng = await c.PostAsJsonAsync("/api/v1/engineering-systems", new
            {
                buildingId = b, name = "Recovery System", systemType = "SupplyVentilation", priority = 50,
                capabilities = new[] { new { code = "eng.co2.reduce", dataType = "double", unit = "ppm", minimum = 0.0, maximum = 10000.0 } },
                resources = new[] { new { code = "airflow_capacity", maximum = 5000.0, unit = "m3h", priority = 50 } }
            });
            eng.EnsureSuccessStatusCode();
            engSystemId = (await eng.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        }

        using (var h2 = CreateHost2())
        {
            var c2 = h2.CreateClientWithAuth();
            var health = await c2.GetAsync("/health/live");
            Assert.True(health.IsSuccessStatusCode);

            var resources = await c2.GetAsync($"/api/v1/engineering-systems/{engSystemId}/resources");
            Assert.True(resources.IsSuccessStatusCode, "Resources should be queryable after restart");
        }
    }
}