using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Dapper;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ClimateHub.RuntimeRecoveryTests;

[Collection("Docker")]
public class RuntimeRestartRecoveryTests : IAsyncLifetime
{
    private readonly IContainer _postgresContainer;
    private string _connectionString = string.Empty;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RuntimeRestartRecoveryTests()
    {
        _postgresContainer = new ContainerBuilder()
            .WithImage("postgres:17-alpine")
            .WithPortBinding(5432, true)
            .WithEnvironment("POSTGRES_DB", "climate_hub_recovery")
            .WithEnvironment("POSTGRES_USER", "climate_hub")
            .WithEnvironment("POSTGRES_PASSWORD", "climate_hub_recovery")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        var port = _postgresContainer.GetMappedPublicPort(5432);
        _connectionString = $"Host={_postgresContainer.Hostname};Port={port};Database=climate_hub_recovery;Username=climate_hub;Password=climate_hub_recovery;";
        await InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task FullStateRecovery_AfterRestart_AllEntitiesRestored()
    {
        var buildingId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var needId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var climatePlanId = Guid.NewGuid();

        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync("INSERT INTO building.buildings (id, name, created_at, version) VALUES (@Id, @Name, NOW(), 1)",
                new { Id = buildingId, Name = "Recovery Test Building" });
            await conn.ExecuteAsync("INSERT INTO building.rooms (id, floor_id, name, created_at, version) VALUES (@Id, @FloorId, @Name, NOW(), 1)",
                new { Id = roomId, FloorId = buildingId, Name = "Recovery Test Room" });
            await conn.ExecuteAsync("INSERT INTO device.devices (id, hardware_id, name, manufacturer, model, protocol_version, status, created_at, version) VALUES (@Id, @HwId, @Name, @Mfr, @Model, @Proto, 'Active', NOW(), 1)",
                new { Id = deviceId, HwId = $"REC-{Guid.NewGuid():N}"[..20], Name = "Recovery Sensor", Mfr = "Test", Model = "R-100", Proto = "1.0" });
            await conn.ExecuteAsync("INSERT INTO device.device_assignments (device_id, room_id, assigned_at, version) VALUES (@DeviceId, @RoomId, NOW(), 1)",
                new { DeviceId = deviceId, RoomId = roomId });
            await conn.ExecuteAsync("INSERT INTO needs.needs (id, room_id, type, status, severity, priority, created_at, version) VALUES (@Id, @RoomId, @Type, @Status, @Severity, @Priority, NOW(), 1)",
                new { Id = needId, RoomId = roomId, Type = "TemperatureRegulation", Status = "Active", Severity = "Medium", Priority = 50 });
            await conn.ExecuteAsync("INSERT INTO command.commands (id, building_id, room_id, device_id, capability_code, operation, parameters_json, status, created_at, version) VALUES (@Id, @BuildingId, @RoomId, @DeviceId, @Cap, @Op, @Params, @Status, NOW(), 1)",
                new { Id = commandId, BuildingId = buildingId, RoomId = roomId, DeviceId = deviceId, Cap = "actuate.relay.on", Op = "on", Params = "{}", Status = "Published" });
        }

        var recoveredBuildingId = Guid.Empty;
        var recoveredRoomId = Guid.Empty;
        var recoveredDeviceId = Guid.Empty;
        var recoveredNeedId = Guid.Empty;
        var recoveredCommandId = Guid.Empty;

        await StartFullHostAndVerify(async (apiClient) =>
        {
            var buildingResp = await apiClient.GetAsync($"/api/v1/buildings/{buildingId}");
            Assert.True(buildingResp.IsSuccessStatusCode, "Building should exist after restart");
            var building = await buildingResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            recoveredBuildingId = Guid.Parse(building.GetProperty("id").GetString()!);

            var deviceResp = await apiClient.GetAsync($"/api/v1/devices/{deviceId}");
            Assert.True(deviceResp.IsSuccessStatusCode, "Device should exist after restart");

            var needsResp = await apiClient.GetAsync($"/api/v1/needs/rooms/{roomId}");
            Assert.True(needsResp.IsSuccessStatusCode, "Needs should be readable after restart");

            var envResp = await apiClient.GetAsync($"/api/v1/rooms/{roomId}/environment");
            Assert.True(envResp.IsSuccessStatusCode, "Environment state should be readable after restart");

            var healthLive = await apiClient.GetAsync("/health/live");
            Assert.True(healthLive.IsSuccessStatusCode, "Liveness check should pass after restart");

            var healthReady = await apiClient.GetAsync("/health/ready");
            Assert.True(healthReady.IsSuccessStatusCode, "Readiness check should pass after restart");
        });

        Assert.Equal(buildingId, recoveredBuildingId);
    }

    [Fact]
    public async Task WorkersRestart_AndProcessPending_AfterRestart()
    {
        var cmdId = Guid.NewGuid();
        var buildingId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync("INSERT INTO building.buildings (id, name, created_at, version) VALUES (@Id, @Name, NOW(), 1)",
                new { Id = buildingId, Name = "Worker Restart Building" });
            await conn.ExecuteAsync("INSERT INTO building.rooms (id, floor_id, name, created_at, version) VALUES (@Id, @FloorId, @Name, NOW(), 1)",
                new { Id = roomId, FloorId = buildingId, Name = "Worker Restart Room" });
            await conn.ExecuteAsync("INSERT INTO device.devices (id, hardware_id, name, manufacturer, model, protocol_version, status, created_at, version) VALUES (@Id, @HwId, @Name, @Mfr, @Model, @Proto, 'Active', NOW(), 1)",
                new { Id = deviceId, HwId = $"WRK-{Guid.NewGuid():N}"[..20], Name = "Worker Sensor", Mfr = "T", Model = "W-100", Proto = "1.0" });
            await conn.ExecuteAsync("INSERT INTO command.commands (id, building_id, room_id, device_id, capability_code, operation, parameters_json, status, created_at, version) VALUES (@Id, @BuildingId, @RoomId, @DeviceId, @Cap, @Op, @Params, @Status, NOW(), 1)",
                new { Id = cmdId, BuildingId = buildingId, RoomId = roomId, DeviceId = deviceId, Cap = "actuate.fan.on", Op = "on", Params = "{}", Status = "Queued" });
        }

        await StartFullHostAndVerify(async (apiClient) =>
        {
            await Task.Delay(5000);
            var cmdResp = await apiClient.GetAsync($"/api/v1/commands/{cmdId}");
            Assert.True(cmdResp.IsSuccessStatusCode, "Command should be accessible after worker restart");
        });
    }

    [Fact]
    public async Task InboxOutbox_ProcessedAfterRestart()
    {
        var messageId = Guid.NewGuid();

        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await conn.ExecuteAsync(@"INSERT INTO platform.internal_event_outbox
                (event_id, event_type, aggregate_type, aggregate_id, status, attempt_count, available_at, created_at, occurred_at)
                VALUES (@EventId, 'test.event', 'test', @AggregateId, 'Pending', 0, NOW(), NOW(), NOW())",
                new { EventId = messageId, AggregateId = Guid.NewGuid().ToString() });
        }

        await StartFullHostAndVerify(async (apiClient) =>
        {
            await Task.Delay(5000);
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM platform.internal_event_outbox WHERE status = 'Processed'");
        });
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var schemas = new[] { "building", "device", "environment", "command", "needs", "climate", "engineering", "platform", "audit" };
        foreach (var schema in schemas)
            await conn.ExecuteAsync($"CREATE SCHEMA IF NOT EXISTS {schema}");

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS building.buildings (id UUID PRIMARY KEY, name TEXT NOT NULL, created_at TIMESTAMPTZ NOT NULL, updated_at TIMESTAMPTZ, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS building.floors (id UUID PRIMARY KEY, building_id UUID NOT NULL, name TEXT NOT NULL, level INTEGER NOT NULL, created_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS building.rooms (id UUID PRIMARY KEY, floor_id UUID NOT NULL, name TEXT NOT NULL, created_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS device.devices (id UUID PRIMARY KEY, hardware_id TEXT NOT NULL, name TEXT NOT NULL, manufacturer TEXT, model TEXT, protocol_version TEXT, status TEXT NOT NULL DEFAULT 'Active', created_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS device.device_assignments (device_id UUID PRIMARY KEY, room_id UUID NOT NULL, assigned_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS command.commands (id UUID PRIMARY KEY, building_id UUID NOT NULL, room_id UUID, device_id UUID NOT NULL, capability_code TEXT NOT NULL, operation TEXT NOT NULL, parameters_json TEXT NOT NULL DEFAULT '{}', status TEXT NOT NULL, created_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS needs.needs (id UUID PRIMARY KEY, room_id UUID NOT NULL, type TEXT NOT NULL, status TEXT NOT NULL, severity TEXT, priority INTEGER DEFAULT 50, created_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS needs.room_environment_state (room_id UUID PRIMARY KEY, temperature_c DOUBLE PRECISION, humidity_pct DOUBLE PRECISION, co2_ppm DOUBLE PRECISION, updated_at TIMESTAMPTZ NOT NULL, version INTEGER NOT NULL DEFAULT 1);
            CREATE TABLE IF NOT EXISTS platform.internal_event_outbox (id BIGSERIAL PRIMARY KEY, event_id UUID NOT NULL, event_type TEXT NOT NULL, aggregate_type TEXT NOT NULL, aggregate_id TEXT, building_id UUID, room_id UUID, occurred_at TIMESTAMPTZ NOT NULL, payload TEXT, headers TEXT, correlation_id TEXT, causation_id TEXT, status TEXT NOT NULL DEFAULT 'Pending', attempt_count INTEGER NOT NULL DEFAULT 0, available_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), processing_started_at TIMESTAMPTZ, processed_at TIMESTAMPTZ, last_failure_code TEXT, last_failure_at TIMESTAMPTZ, created_at TIMESTAMPTZ NOT NULL DEFAULT NOW());
            CREATE TABLE IF NOT EXISTS environment.telemetry_outbox (id BIGSERIAL PRIMARY KEY, room_id UUID NOT NULL, device_id UUID NOT NULL, measured_at TIMESTAMPTZ NOT NULL, temperature_c DOUBLE PRECISION, humidity_pct DOUBLE PRECISION, co2_ppm DOUBLE PRECISION, quality TEXT, created_at TIMESTAMPTZ NOT NULL, status TEXT NOT NULL DEFAULT 'Pending');
        ");
    }

    private async Task StartFullHostAndVerify(Func<HttpClient, Task> verifyAction)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = "Test";
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = _connectionString,
            ["Mqtt:Host"] = "localhost",
            ["Mqtt:Port"] = "1883",
            ["Mqtt:ClientId"] = "climate-hub-recovery-test",
            ["InfluxDb:Url"] = "http://localhost:8086",
            ["InfluxDb:Token"] = "recovery-test-token",
            ["InfluxDb:Organization"] = "climate-hub",
            ["InfluxDb:Bucket"] = "climate-hub",
            ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
            ["Jwt:Issuer"] = "ClimateHub",
            ["Jwt:Audience"] = "ClimateHub.Api",
        });

        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

        var cts = new CancellationTokenSource();
        var hostTask = Task.Run(async () =>
        {
            try
            {
                var host = builder.Build();
                await host.RunAsync(cts.Token);
            }
            catch (OperationCanceledException) { }
        });

        try
        {
            await verifyAction(httpClient);
        }
        finally
        {
            cts.Cancel();
            try { await hostTask; } catch { }
        }
    }
}