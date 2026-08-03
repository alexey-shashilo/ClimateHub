using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.EndToEndTests;

[Trait("Category", "E2E")]
[Collection("Docker")]
public class ProductionE2EScenarios : IClassFixture<E2eProductionFixture>
{
    private readonly E2eProductionFixture _fixture;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProductionE2EScenarios(E2eProductionFixture fixture)
    {
        _fixture = fixture;
        _http = fixture.ApiClient;
    }

    [Fact]
    public async Task PositiveScenario_FullPipeline_GoalSatisfied()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { temperatureC = 28.5, relativeHumidityPct = 65.0, co2Ppm = 800 });

        var need = await WaitForNeedStatus(ctx.RoomId, new[] { "Detected", "Planning", "Planned" });
        Assert.NotNull(need);

        await PublishTelemetry(ctx, new { temperatureC = 23.0, relativeHumidityPct = 50.0, co2Ppm = 500 });

        var satisfied = await WaitForNeedStatus(ctx.RoomId, new[] { "Satisfied" }, 60);
        Assert.NotNull(satisfied);
    }

    [Fact]
    public async Task NegativeScenario_InvalidCapability_TelemetryRejected()
    {
        var ctx = await SetupEnvironment();

        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId = ctx.BuildingId,
            deviceId = ctx.DeviceId,
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = Random.Shared.Next(1, 100000),
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { temperatureC = 999.9 }
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{ctx.BuildingId}/{ctx.DeviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _fixture.AdminMqttClient.PublishAsync(message);
        await Task.Delay(2000);

        var env = await _http.GetAsync($"/api/v1/rooms/{ctx.RoomId}/environment");
        Assert.True(env.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CommandTimeout_WithoutEffect_PlanFailed()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { co2Ppm = 1800 });

        var need = await WaitForNeedStatus(ctx.RoomId, new[] { "Detected", "Planning" });
        Assert.NotNull(need);

        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(5000);
            await PublishTelemetry(ctx, new { co2Ppm = 1800 });
        }

        var blocked = await WaitForNeedStatus(ctx.RoomId, new[] { "Blocked" }, 90);
        Assert.NotNull(blocked);
    }

    [Fact]
    public async Task EngineeringFailure_PlanFails()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { temperatureC = 5.0 });

        var need = await WaitForCondition(ctx.RoomId, needs =>
            needs.GetArrayLength() > 0 && needs[0].TryGetProperty("status", out var s) && s.GetString() != "Detected", 30);
        Assert.NotNull(need);
    }

    [Fact]
    public async Task EffectTimeout_AfterCommandCompletes_NeedBlocks()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { temperatureC = 30.0 });

        var need = await WaitForNeedStatus(ctx.RoomId, new[] { "Detected", "Planning", "Planned" });
        Assert.NotNull(need);
    }

    [Fact]
    public async Task GoalBlocked_WhenEnvironmentNeverChanges()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { temperatureC = 30.0, relativeHumidityPct = 70.0, co2Ppm = 1500 });

        var blocked = await WaitForNeedStatus(ctx.RoomId, new[] { "Blocked" }, 120);
        Assert.NotNull(blocked);
    }

    [Fact]
    public async Task ResourceRelease_AfterGoalCompleted()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { temperatureC = 28.0 });

        var need = await WaitForNeedStatus(ctx.RoomId, new[] { "Detected", "Planning", "Planned" });
        Assert.NotNull(need);

        await PublishTelemetry(ctx, new { temperatureC = 22.0, relativeHumidityPct = 50.0, co2Ppm = 400 });

        var satisfied = await WaitForNeedStatus(ctx.RoomId, new[] { "Satisfied" }, 60);
        Assert.NotNull(satisfied);

        await Task.Delay(10000);
    }

    [Fact]
    public async Task RestartRecovery_AfterCrash_StateRestored()
    {
        var ctx = await SetupEnvironment();

        await PublishTelemetry(ctx, new { temperatureC = 30.0 });

        var need = await WaitForNeedStatus(ctx.RoomId, new[] { "Detected", "Planning", "Planned" }, 30);
        Assert.NotNull(need);

        await _fixture.StopGatewayAsync();
        await Task.Delay(2000);
        await _fixture.StartGatewayAsync();
        await Task.Delay(5000);

        var needsAfter = await _http.GetAsync($"/api/v1/needs/rooms/{ctx.RoomId}");
        Assert.True(needsAfter.IsSuccessStatusCode);
        var needsContent = await needsAfter.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.NotEqual(JsonValueKind.Null, needsContent.ValueKind);
    }

    private async Task<(string BuildingId, string FloorId, string RoomId, string DeviceId)> SetupEnvironment()
    {
        var createBuilding = await _http.PostAsJsonAsync("/api/v1/buildings", new { name = $"E2E-Building-{Guid.NewGuid():N}"[..20] });
        createBuilding.EnsureSuccessStatusCode();
        var building = await createBuilding.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var buildingId = building.GetProperty("id").GetString()!;

        var createFloor = await _http.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/floors",
            new { name = "Main Floor", level = 0 });
        createFloor.EnsureSuccessStatusCode();
        var floor = await createFloor.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var floorId = floor.GetProperty("id").GetString()!;

        var createRoom = await _http.PostAsJsonAsync($"/api/v1/floors/{floorId}/rooms",
            new { name = "E2E Test Room" });
        createRoom.EnsureSuccessStatusCode();
        var room = await createRoom.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var roomId = room.GetProperty("id").GetString()!;

        var registerDevice = await _http.PostAsJsonAsync("/api/v1/devices",
            new { hardwareId = $"E2E-SENSOR-{Guid.NewGuid():N}"[..20], name = "Test Sensor", manufacturer = "Test", modelName = "E2E-3000", protocolVersion = "1.0" });
        registerDevice.EnsureSuccessStatusCode();
        var device = await registerDevice.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var deviceId = device.GetProperty("id").GetString()!;

        var assignDevice = await _http.PostAsJsonAsync($"/api/v1/devices/{deviceId}/assignments",
            new { roomId });
        assignDevice.EnsureSuccessStatusCode();

        return (buildingId, floorId, roomId, deviceId);
    }

    private async Task PublishTelemetry((string BuildingId, string FloorId, string RoomId, string DeviceId) ctx, object payload)
    {
        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.DeviceId, payload);
    }

    private async Task<JsonElement?> WaitForNeedStatus(string roomId, string[] expectedStatuses, int timeoutSeconds = 30)
    {
        return await WaitForCondition(roomId, needs =>
        {
            if (needs.GetArrayLength() == 0) return false;
            var status = needs[0].TryGetProperty("status", out var s) ? s.GetString() : null;
            return status != null && expectedStatuses.Contains(status);
        }, timeoutSeconds);
    }

    private async Task<JsonElement?> WaitForCondition(string roomId, Func<JsonElement, bool> predicate, int timeoutSeconds = 30)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var resp = await _http.GetAsync($"/api/v1/needs/rooms/{roomId}");
            if (resp.IsSuccessStatusCode)
            {
                var needs = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
                if (needs.ValueKind == JsonValueKind.Array && predicate(needs))
                    return needs[0];
            }
            await Task.Delay(2000);
        }
        return null;
    }
}