using System.Net.Http.Json;
using System.Text.Json;

namespace ClimateHub.EndToEndTests;

[Trait("Category", "E2E")]
[Collection("Docker")]
public class ProductionE2EScenarios : IClassFixture<E2eProductionFixture>
{
    private readonly E2eProductionFixture _fixture;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public ProductionE2EScenarios(E2eProductionFixture fixture)
    {
        _fixture = fixture;
        _http = fixture.ApiClient;
    }

    [Fact]
    public async Task ClosedLoop_Co2_FullOrchestration()
    {
        var ctx = await SetupFullEngineeringRuntime();
        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 1800.0 });

        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Detected", 30));
        Assert.NotNull(await Eventually(() => GetClimateGoal(ctx.RoomId), g => g?.Status is "Active" or "Planning", 20));
        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status is "Planning" or "Planned", 30));

        bool engPlans = await EventuallyTrue(async () => (await GetCommandPlans(ctx.EngSystemId)).Count > 0, 20);
        Assert.True(engPlans);

        bool engActive = await EventuallyTrue(async () => (await GetEngSystemStatus(ctx.EngSystemId))?.ActivePlanCount > 0, 15);
        Assert.True(engActive);

        bool resReserved = await EventuallyTrue(async () => (await GetEngResources(ctx.EngSystemId)).Any(r => r.Reserved > 0), 20);
        Assert.True(resReserved);

        bool cmdRecv = await EventuallyTrue(() => Task.FromResult(ctx.Actuator.ReceivedCommands.Count > 0), 20);
        Assert.True(cmdRecv);

        Assert.NotNull(await Eventually(() => GetCommand(ctx.RoomId), c => c?.Status is "Acknowledged" or "Executing" or "Succeeded", 15));
        Assert.NotNull(await Eventually(() => GetCommand(ctx.RoomId), c => c?.Status is "Executing" or "Succeeded", 15));
        Assert.NotNull(await Eventually(() => GetCommand(ctx.RoomId), c => c?.Status == "Succeeded", 15));

        bool waiting = await EventuallyTrue(async () => (await GetCommandPlans(ctx.EngSystemId)).Any(p => p.Status is "Executing" or "Planned"), 20);
        Assert.True(waiting);

        bool envOk = await EventuallyTrue(() => GetEnvCo2Below(ctx.RoomId, 1000), 30);
        Assert.True(envOk);

        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Satisfied", 60));
        Assert.NotNull(await Eventually(() => GetClimateGoal(ctx.RoomId), g => g?.Status == "Completed", 20));

        bool released = await EventuallyTrue(async () => (await GetEngResources(ctx.EngSystemId)).All(r => r.Reserved == 0 && r.Used == 0), 20);
        Assert.True(released);
    }

    [Fact]
    public async Task ActuatorCommandFailed_NeedBlocked()
    {
        var ctx = await SetupFullEngineeringRuntime(ActuatorMode.CommandFailed);
        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 2000.0 });
        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Detected", 30));
        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Blocked", 90));
    }

    [Fact]
    public async Task RejectedCommand_NeedBlocked()
    {
        var ctx = await SetupFullEngineeringRuntime(ActuatorMode.Rejected);
        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 2000.0 });
        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Detected", 30));
        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Blocked", 90));
    }

    [Fact]
    public async Task NegativeScenario_InvalidTelemetry_Rejected()
    {
        var ctx = await SetupFullEngineeringRuntime();
        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { temperatureC = 999.9 });

        await Task.Delay(3000);
        var env = await GetEnvironment(ctx.RoomId);
        bool hasTemp = env.TryGetValue("temperatureC", out var t) && t.HasValue;
        Assert.False(hasTemp, "Environment should NOT be updated with invalid temperature");
        var needs = await GetNeeds(ctx.RoomId);
        Assert.Empty(needs);
    }

    [Fact]
    public async Task ResourceLifecycle_FullReleaseAfterSatisfied()
    {
        var ctx = await SetupFullEngineeringRuntime();
        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 1800.0 });

        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status is "Detected" or "Planning" or "Planned", 30));
        bool resReserved = await EventuallyTrue(async () => (await GetEngResources(ctx.EngSystemId)).Any(r => r.Reserved > 0), 20);
        Assert.True(resReserved);

        bool cmdRecv = await EventuallyTrue(() => Task.FromResult(ctx.Actuator.ReceivedCommands.Count > 0), 20);
        Assert.True(cmdRecv);

        await _fixture.PublishTelemetryAsync(ctx.BuildingId, ctx.SensorDeviceId, new { co2Ppm = 450.0 });

        Assert.NotNull(await Eventually(() => GetNeed(ctx.RoomId), n => n?.Status == "Satisfied", 60));
        bool released = await EventuallyTrue(async () => (await GetEngResources(ctx.EngSystemId)).All(r => r.Reserved == 0 && r.Used == 0), 30);
        Assert.True(released);
    }

    // ─── Setup ───────────────────────────────────────────────

    private async Task<E2eContext> SetupFullEngineeringRuntime(ActuatorMode mode = ActuatorMode.Successful)
    {
        var ctx = new E2eContext();
        
        // Verify API is accessible first
        var live = await _http.GetAsync("/health/live");
        Assert.True(live.IsSuccessStatusCode, "Health check should pass first");
        
        var building = await PostRead("/api/v1/buildings", new { name = $"E2E-B-{Guid.NewGuid():N}"[..15] });
        ctx.BuildingId = ReqStr(building, "id");
        var floor = await PostRead($"/api/v1/buildings/{ctx.BuildingId}/floors", new { name = "Ground", level = 0 });
        var room = await PostRead($"/api/v1/floors/{ReqStr(floor, "id")}/rooms", new { name = "E2E Run Room" });
        ctx.RoomId = ReqStr(room, "id");

        await _http.PutAsJsonAsync($"/api/v1/rooms/{ctx.RoomId}/policy", new
        {
            co2 = new { minimum = 0.0, maximum = 1000.0, preferred = 600.0, controlMode = "Automatic" },
            temperature = new { minimum = 18.0, maximum = 28.0, preferred = 23.0, controlMode = "MonitorOnly" },
            humidity = new { minimum = 30.0, maximum = 60.0, preferred = 45.0, controlMode = "MonitorOnly" }
        });

        var sensor = await PostRead("/api/v1/devices", new
        {
            hardwareId = $"SENSOR-{Guid.NewGuid():N}"[..20], name = "CO2 Sensor", manufacturer = "E2E", modelName = "CS-100", protocolVersion = "1.0"
        });
        ctx.SensorDeviceId = ReqStr(sensor, "id");
        await _http.PostAsJsonAsync($"/api/v1/devices/{ctx.SensorDeviceId}/assignments", new { roomId = ctx.RoomId });

        var fanDevice = await PostRead("/api/v1/devices", new
        {
            hardwareId = $"FAN-{Guid.NewGuid():N}"[..20], name = "Supply Fan", manufacturer = "E2E", modelName = "SF-200", protocolVersion = "1.0"
        });
        ctx.FanDeviceId = ReqStr(fanDevice, "id");
        await _http.PostAsJsonAsync($"/api/v1/devices/{ctx.FanDeviceId}/assignments", new { roomId = ctx.RoomId });

        var engSystem = await PostRead("/api/v1/engineering-systems", new
        {
            buildingId = ctx.BuildingId, name = "Supply Ventilation", systemType = "SupplyVentilation", priority = 50,
            capabilities = new[] { new { code = "eng.co2.reduce", dataType = "double", unit = "ppm", minimum = 0.0, maximum = 10000.0 }, new { code = "eng.airflow.increase", dataType = "double", unit = "m3h", minimum = 0.0, maximum = 10000.0 } },
            resources = new[] { new { code = "airflow_capacity", maximum = 5000.0, unit = "m3h", priority = 50 } }
        });
        ctx.EngSystemId = ReqStr(engSystem, "id");

        ctx.Actuator = new TestActuatorRuntime(_fixture.MqttHost, _fixture.MqttPort, ctx.BuildingId, ctx.FanDeviceId, mode);
        await ctx.Actuator.StartAsync();
        return ctx;
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static async Task<T?> Eventually<T>(Func<Task<T?>> poll, Func<T?, bool> cond, int sec) where T : class
        => await TestPolling.EventuallyAsync(poll, cond, TimeSpan.FromSeconds(sec));

    private static async Task<bool> EventuallyTrue(Func<Task<bool>> poll, int sec)
        => await TestPolling.EventuallyTrueAsync(poll, TimeSpan.FromSeconds(sec));

    private async Task<NeedDto?> GetNeed(string roomId)
    {
        var json = await GetJson($"/api/v1/needs/rooms/{roomId}");
        if (json is null) return null;
        var j = json.Value;
        if (j.ValueKind != JsonValueKind.Array || j.GetArrayLength() == 0) return null;
        return AsNeed(j[0]);
    }

    private async Task<List<NeedDto>> GetNeeds(string roomId)
    {
        var json = await GetJson($"/api/v1/needs/rooms/{roomId}");
        if (json is null) return new();
        var j = json.Value;
        if (j.ValueKind != JsonValueKind.Array) return new();
        var list = new List<NeedDto>();
        foreach (var i in j.EnumerateArray()) list.Add(AsNeed(i));
        return list;
    }

    private async Task<ClimateGoalDto?> GetClimateGoal(string roomId)
    {
        var json = await GetJson($"/api/v1/climate/goals/{roomId}");
        if (json is null) return null;
        var j = json.Value;
        return new ClimateGoalDto { Status = PropStr(j, "Status") ?? PropStr(j, "status") };
    }

    private async Task<CommandDto?> GetCommand(string roomId)
    {
        var json = await GetJson($"/api/v1/rooms/{roomId}/commands");
        if (json is null) return null;
        var j = json.Value;
        if (j.ValueKind != JsonValueKind.Array || j.GetArrayLength() == 0) return null;
        return AsCommand(j[0]);
    }

    private async Task<Dictionary<string, double?>> GetEnvironment(string roomId)
    {
        var json = await GetJson($"/api/v1/rooms/{roomId}/environment");
        if (json is null) return new();
        var j = json.Value;
        var d = new Dictionary<string, double?>();
        foreach (var p in j.EnumerateObject())
            d[p.Name] = p.Value.ValueKind == JsonValueKind.Number ? p.Value.GetDouble() : null;
        return d;
    }

    private async Task<bool> GetEnvCo2Below(string roomId, double threshold)
    {
        var env = await GetEnvironment(roomId);
        return env.TryGetValue("co2Ppm", out var c) && c.HasValue && c.Value < threshold;
    }

    private async Task<EngSystemStatusDto?> GetEngSystemStatus(string? engSystemId)
    {
        if (engSystemId is null) return null;
        var json = await GetJson($"/api/v1/engineering-systems/{engSystemId}/status");
        if (json is null) return null;
        var j = json.Value;
        return new EngSystemStatusDto { ActivePlanCount = j.TryGetProperty("activePlanCount", out var a) ? a.GetInt32() : 0 };
    }

    private async Task<List<ResourceDto>> GetEngResources(string? engSystemId)
    {
        if (engSystemId is null) return new();
        var json = await GetJson($"/api/v1/engineering-systems/{engSystemId}/resources");
        if (json is null) return new();
        var j = json.Value;
        if (j.ValueKind != JsonValueKind.Array) return new();
        var list = new List<ResourceDto>();
        foreach (var i in j.EnumerateArray())
            list.Add(new ResourceDto { Code = PropStr(i, "code"), Reserved = Num(i, "reserved"), Used = Num(i, "used") });
        return list;
    }

    private async Task<List<CommandPlanDto>> GetCommandPlans(string? engSystemId)
    {
        if (engSystemId is null) return new();
        var json = await GetJson($"/api/v1/engineering-systems/{engSystemId}/plans");
        if (json is null) return new();
        var j = json.Value;
        if (j.ValueKind != JsonValueKind.Array) return new();
        var list = new List<CommandPlanDto>();
        foreach (var i in j.EnumerateArray())
            list.Add(new CommandPlanDto { Status = PropStr(i, "status") ?? PropStr(i, "Status") });
        return list;
    }

    private async Task<JsonElement?> GetJson(string url)
    {
        var resp = await _http.GetAsync(url);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private async Task<JsonElement> PostRead(string url, object body)
    {
        var resp = await _http.PostAsJsonAsync(url, body);
        if (!resp.IsSuccessStatusCode)
        {
            var bodyText = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"PostRead {url}: {(int)resp.StatusCode} {resp.ReasonPhrase} — {bodyText}");
            resp.EnsureSuccessStatusCode();
        }
        return await resp.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    private static NeedDto AsNeed(JsonElement e) => new() { Id = PropStr(e, "id"), Status = PropStr(e, "status"), Type = PropStr(e, "type") };
    private static CommandDto AsCommand(JsonElement e) => new() { Id = PropStr(e, "id"), Status = PropStr(e, "status") ?? PropStr(e, "Status") };
    private static string ReqStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private static string? PropStr(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static double Num(JsonElement e, string p) => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private record E2eContext { public string BuildingId { get; set; } = ""; public string RoomId { get; set; } = ""; public string SensorDeviceId { get; set; } = ""; public string FanDeviceId { get; set; } = ""; public string? EngSystemId { get; set; } public TestActuatorRuntime Actuator { get; set; } = null!; }
    private record NeedDto(string? Id = null, string? Status = null, string? Type = null);
    private record ClimateGoalDto(string? Status = null);
    private record CommandDto(string? Id = null, string? Status = null);
    private record CommandPlanDto(string? Status = null);
    private record EngSystemStatusDto(int ActivePlanCount = 0);
    private record ResourceDto(string? Code = null, double Reserved = 0, double Used = 0);
}