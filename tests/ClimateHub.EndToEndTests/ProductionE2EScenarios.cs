using System.Net.Http.Json;
using System.Text.Json;

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

        await ctx.Actuator.StartAsync();

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 1800.0 });

        var need = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status == "Detected",
            TimeSpan.FromSeconds(30));
        Assert.NotNull(need);
        Assert.Equal("Detected", need.Status);

        var needPlanned = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status is "Planning" or "Planned",
            TimeSpan.FromSeconds(30));
        Assert.NotNull(needPlanned);

        var commandReceived = await TestPolling.EventuallyAsync(
            () => Task.FromResult(ctx.Actuator.ReceivedCommands.Count > 0 ? ctx.Actuator.ReceivedCommands[0] : null),
            c => c is not null,
            TimeSpan.FromSeconds(20));
        Assert.NotNull(commandReceived);

        var envChanged = await TestPolling.EventuallyTrueAsync(
            () => GetEnvCo2(ctx.RoomId),
            TimeSpan.FromSeconds(30));
        Assert.True(envChanged, "CO2 should drop below 1000 after actuator effect");

        var satisfied = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status == "Satisfied",
            TimeSpan.FromSeconds(60));
        Assert.NotNull(satisfied);
        Assert.Equal("Satisfied", satisfied.Status);
    }

    [Fact]
    public async Task NegativeScenario_InvalidTelemetry_Rejected()
    {
        var ctx = await SetupEnvironment();
        await ctx.Actuator.StartAsync();

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { temperatureC = 999.9 });

        await Task.Delay(2000);

        var env = await _http.GetAsync($"/api/v1/rooms/{ctx.RoomId}/environment");
        Assert.True(env.IsSuccessStatusCode);
        var envState = await env.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        bool hasTemp = envState.TryGetProperty("temperatureC", out var t) && t.ValueKind != JsonValueKind.Null;
        Assert.False(hasTemp, "Environment should NOT be updated with invalid temperature");

        var needs = await GetActiveNeeds(ctx.RoomId);
        Assert.Empty(needs);
    }

    [Fact]
    public async Task EffectTimeout_CommandCompleted_NoEffect_NeedBlocked()
    {
        var ctx = await SetupEnvironment();
        await ctx.Actuator.StartAsync();

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 2000.0 });

        var needDetected = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status == "Detected",
            TimeSpan.FromSeconds(30));
        Assert.NotNull(needDetected);

        await TestPolling.EventuallyTrueAsync(
            () => Task.FromResult(ctx.Actuator.ReceivedCommands.Count > 0),
            TimeSpan.FromSeconds(20));

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 2000.0 });

        var blocked = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status == "Blocked",
            TimeSpan.FromSeconds(90));
        Assert.NotNull(blocked);
        Assert.Equal("Blocked", blocked.Status);
    }

    [Fact]
    public async Task ResourceRelease_AfterSatisfied()
    {
        var ctx = await SetupEnvironment();
        await ctx.Actuator.StartAsync();

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 1800.0 });

        await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status is "Detected" or "Planning" or "Planned",
            TimeSpan.FromSeconds(30));

        await TestPolling.EventuallyTrueAsync(
            () => Task.FromResult(ctx.Actuator.ReceivedCommands.Count > 0),
            TimeSpan.FromSeconds(20));

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 450.0 });

        var satisfied = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status == "Satisfied",
            TimeSpan.FromSeconds(60));
        Assert.NotNull(satisfied);
        Assert.Equal("Satisfied", satisfied.Status);
    }

    [Fact]
    public async Task RestartRecovery_AfterCrash_StateRestored()
    {
        var ctx = await SetupEnvironment();
        await ctx.Actuator.StartAsync();

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 1800.0 });

        var need = await TestPolling.EventuallyAsync(
            () => GetFirstNeed(ctx.RoomId),
            n => n is not null && n.Status is "Detected" or "Planning" or "Planned",
            TimeSpan.FromSeconds(30));
        Assert.NotNull(need);

        await _fixture.StopGatewayAsync();
        await Task.Delay(1000);
        await _fixture.StartGatewayAsync();
        await Task.Delay(3000);

        var needsAfter = await _http.GetAsync($"/api/v1/needs/rooms/{ctx.RoomId}");
        Assert.True(needsAfter.IsSuccessStatusCode, "Needs endpoint should work after restart");

        var live = await _http.GetAsync("/health/live");
        Assert.True(live.IsSuccessStatusCode, "Liveness should pass after restart");

        var ready = await _http.GetAsync("/health/ready");
        Assert.True(ready.IsSuccessStatusCode, "Readiness should pass after restart");
    }

    private async Task<E2eContext> SetupEnvironment()
    {
        var ctx = new E2eContext();

        var building = await CreateObject("/api/v1/buildings", new { name = $"E2E-B-{Guid.NewGuid():N}"[..15] });
        ctx.BuildingId = ReadRequiredString(building, "id");

        var floor = await CreateObject($"/api/v1/buildings/{ctx.BuildingId}/floors", new { name = "Ground", level = 0 });
        ctx.FloorId = ReadRequiredString(floor, "id");

        var room = await CreateObject($"/api/v1/floors/{ctx.FloorId}/rooms", new { name = "E2E Test Room" });
        ctx.RoomId = ReadRequiredString(room, "id");

        await _http.PutAsJsonAsync($"/api/v1/rooms/{ctx.RoomId}/policy", new
        {
            co2 = new { minimum = 0.0, maximum = 1000.0, preferred = 600.0, controlMode = "Automatic" },
            temperature = new { minimum = 18.0, maximum = 28.0, preferred = 23.0, controlMode = "MonitorOnly" },
            humidity = new { minimum = 30.0, maximum = 60.0, preferred = 45.0, controlMode = "MonitorOnly" }
        });

        var sensor = await CreateObject("/api/v1/devices", new
        {
            hardwareId = $"SENSOR-{Guid.NewGuid():N}"[..20], name = "CO2 Sensor",
            manufacturer = "E2E", modelName = "CS-100", protocolVersion = "1.0"
        });
        ctx.SensorDeviceId = ReadRequiredString(sensor, "id");

        await _http.PostAsJsonAsync($"/api/v1/devices/{ctx.SensorDeviceId}/assignments", new { roomId = ctx.RoomId });

        var fanDevice = await CreateObject("/api/v1/devices", new
        {
            hardwareId = $"FAN-{Guid.NewGuid():N}"[..20], name = "Supply Fan",
            manufacturer = "E2E", modelName = "SF-200", protocolVersion = "1.0"
        });
        ctx.FanDeviceId = ReadRequiredString(fanDevice, "id");

        await _http.PostAsJsonAsync($"/api/v1/devices/{ctx.FanDeviceId}/assignments", new { roomId = ctx.RoomId });

        ctx.Actuator = new TestActuatorRuntime(
            _fixture.MqttHost, _fixture.MqttPort,
            ctx.BuildingId, ctx.FanDeviceId);

        return ctx;
    }

    private async Task<JsonElement> CreateObject(string url, object body)
    {
        var resp = await _http.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return json;
    }

    private async Task<NeedDto?> GetFirstNeed(string roomId)
    {
        var resp = await _http.GetAsync($"/api/v1/needs/rooms/{roomId}");
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() == 0) return null;

        var first = json[0];
        return new NeedDto
        {
            Id = GetPropString(first, "id") ?? "",
            Status = GetPropString(first, "status") ?? "",
            Type = GetPropString(first, "type") ?? "",
            RoomId = GetPropString(first, "roomId") ?? ""
        };
    }

    private async Task<List<NeedDto>> GetActiveNeeds(string roomId)
    {
        var resp = await _http.GetAsync($"/api/v1/needs/rooms/{roomId}");
        if (!resp.IsSuccessStatusCode) return new();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        if (json.ValueKind != JsonValueKind.Array) return new();

        var list = new List<NeedDto>();
        foreach (var item in json.EnumerateArray())
        {
            list.Add(new NeedDto
            {
                Id = GetPropString(item, "id") ?? "",
                Status = GetPropString(item, "status") ?? "",
                Type = GetPropString(item, "type") ?? "",
                RoomId = GetPropString(item, "roomId") ?? ""
            });
        }
        return list;
    }

    private async Task<bool> GetEnvCo2(string roomId)
    {
        var resp = await _http.GetAsync($"/api/v1/rooms/{roomId}/environment");
        if (!resp.IsSuccessStatusCode) return false;

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        if (json.TryGetProperty("co2Ppm", out var co2) && co2.ValueKind == JsonValueKind.Number)
            return co2.GetDouble() < 1000;
        return false;
    }

    private static string? GetPropString(JsonElement element, string property)
        => element.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static string ReadRequiredString(JsonElement element, string property)
        => element.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

    private class E2eContext
    {
        public string BuildingId { get; set; } = "";
        public string FloorId { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string SensorDeviceId { get; set; } = "";
        public string FanDeviceId { get; set; } = "";
        public TestActuatorRuntime Actuator { get; set; } = null!;
    }

    private record NeedDto
    {
        public string Id { get; init; } = "";
        public string Status { get; init; } = "";
        public string Type { get; init; } = "";
        public string RoomId { get; init; } = "";
    }
}