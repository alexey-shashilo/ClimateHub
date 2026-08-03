using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Protocol;

namespace ClimateHub.EndToEndTests;

[Trait("Category", "E2E")]
[Collection("Docker")]
public class EndToEndScenario : IClassFixture<EndToEndFixture>
{
    private readonly EndToEndFixture _fixture;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EndToEndScenario(EndToEndFixture fixture)
    {
        _fixture = fixture;
        _http = fixture.CreateClient();
    }

    [Fact]
    public async Task HighCo2_CreatesNeed_AndNormalCo2_SatisfiesIt()
    {
        var (buildingId, floorId, roomId, deviceId) = await SetupBuildingInfrastructure();

        var policyId = await CreateCo2Policy(buildingId);

        await PublishHighCo2Telemetry(deviceId, buildingId);

        var need = await WaitForNeedCreated(roomId);
        Assert.NotNull(need);
        Assert.Equal("Co2TooHigh", need.Value.GetProperty("type").GetString());

        await PublishNormalCo2Telemetry(deviceId, buildingId);

        var satisfied = await WaitForNeedSatisfied(roomId);
        Assert.NotNull(satisfied);
        Assert.Equal("Satisfied", satisfied.Value.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CommandWithoutImprovement_TimesOut_PlanFailed()
    {
        var (buildingId, floorId, roomId, deviceId) = await SetupBuildingInfrastructure();
        var policyId = await CreateCo2Policy(buildingId);

        await PublishHighCo2Telemetry(deviceId, buildingId);

        var need = await WaitForNeedCreated(roomId);
        Assert.NotNull(need);

        var commandPlan = await WaitForCommandPlan(need.Value);
        Assert.NotNull(commandPlan);

        await PublishHighCo2Telemetry(deviceId, buildingId);

        var failed = await WaitForPlanFailed(commandPlan.Value);
        Assert.NotNull(failed);
        Assert.Equal("Failed", failed.Value.GetProperty("status").GetString());
    }

    private async Task<(string buildingId, string floorId, string roomId, string deviceId)> SetupBuildingInfrastructure()
    {
        var createBuilding = await _http.PostAsJsonAsync("/api/v1/buildings", new { name = "E2E CO2 Building" });
        createBuilding.EnsureSuccessStatusCode();
        var building = await createBuilding.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var buildingId = building.GetProperty("id").GetString()!;

        var createFloor = await _http.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/floors",
            new { name = "Main Floor", level = 0 });
        createFloor.EnsureSuccessStatusCode();
        var floor = await createFloor.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var floorId = floor.GetProperty("id").GetString()!;

        var createRoom = await _http.PostAsJsonAsync($"/api/v1/floors/{floorId}/rooms",
            new { name = "Conference Room A" });
        createRoom.EnsureSuccessStatusCode();
        var room = await createRoom.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var roomId = room.GetProperty("id").GetString()!;

        var registerDevice = await _http.PostAsJsonAsync("/api/v1/devices",
            new { hardwareId = $"E2E-CO2-{Guid.NewGuid():N}"[..20], name = "CO2 Sensor", manufacturer = "Test", modelName = "E2E-2000", protocolVersion = "1.0" });
        registerDevice.EnsureSuccessStatusCode();
        var device = await registerDevice.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var deviceId = device.GetProperty("id").GetString()!;

        var assignDevice = await _http.PostAsJsonAsync($"/api/v1/devices/{deviceId}/assignments",
            new { roomId });
        assignDevice.EnsureSuccessStatusCode();

        return (buildingId, floorId, roomId, deviceId);
    }

    private async Task<string> CreateCo2Policy(string buildingId)
    {
        var createPolicy = await _http.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/policies",
            new
            {
                name = "CO2 Threshold Policy",
                type = "Co2Threshold",
                rule = new { maxCo2Ppm = 1000, evaluationIntervalMinutes = 1 },
                effect = new
                {
                    type = "VentilationIncrease",
                    activationDelayMinutes = 1,
                    durationMinutes = 30
                }
            });
        createPolicy.EnsureSuccessStatusCode();
        var policy = await createPolicy.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return policy.GetProperty("id").GetString()!;
    }

    private async Task<string> PublishHighCo2Telemetry(string deviceId, string buildingId)
    {
        using var mqttClient = await ConnectMqttClient();
        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId,
            deviceId,
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = 1,
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { co2Ppm = 1800 }
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await mqttClient.PublishAsync(message);
        Assert.True(result.IsSuccess);
        return buildingId;
    }

    private async Task PublishNormalCo2Telemetry(string deviceId, string buildingId)
    {
        using var mqttClient = await ConnectMqttClient();
        var telemetry = JsonSerializer.Serialize(new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId,
            deviceId,
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = 2,
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { co2Ppm = 400 }
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await mqttClient.PublishAsync(message);
        Assert.True(result.IsSuccess);
    }

    private async Task<JsonElement?> WaitForNeedCreated(string roomId)
    {
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(1000);
            var resp = await _http.GetAsync($"/api/v1/rooms/{roomId}/needs");
            if (!resp.IsSuccessStatusCode) continue;
            var needs = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            if (needs.ValueKind == JsonValueKind.Array && needs.GetArrayLength() > 0)
                return needs[0];
        }
        return null;
    }

    private async Task<JsonElement?> WaitForNeedSatisfied(string roomId)
    {
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(1000);
            var resp = await _http.GetAsync($"/api/v1/rooms/{roomId}/needs");
            if (!resp.IsSuccessStatusCode) continue;
            var needs = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            if (needs.ValueKind != JsonValueKind.Array || needs.GetArrayLength() == 0) continue;
            var status = needs[0].GetProperty("status").GetString();
            if (status == "Satisfied")
                return needs[0];
        }
        return null;
    }

    private async Task<JsonElement?> WaitForCommandPlan(JsonElement need)
    {
        var needId = need.GetProperty("id").GetString()!;
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(1000);
            var resp = await _http.GetAsync($"/api/v1/needs/{needId}/plans");
            if (!resp.IsSuccessStatusCode) continue;
            var plans = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            if (plans.ValueKind == JsonValueKind.Array && plans.GetArrayLength() > 0)
                return plans[0];
        }
        return null;
    }

    private async Task<JsonElement?> WaitForPlanFailed(JsonElement plan)
    {
        var planId = plan.GetProperty("id").GetString()!;
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(1000);
            var resp = await _http.GetAsync($"/api/v1/plans/{planId}");
            if (!resp.IsSuccessStatusCode) continue;
            var planDetail = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            var status = planDetail.GetProperty("status").GetString();
            if (status == "Failed")
                return planDetail;
        }
        return null;
    }

    private async Task<IMqttClient> ConnectMqttClient()
    {
        var mqttFactory = new MqttClientFactory();
        var client = mqttFactory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", _fixture.MqttPort)
            .WithClientId($"e2e-test-{Guid.NewGuid():N}"[..20])
            .WithCleanSession()
            .Build();

        await client.ConnectAsync(options);
        return client;
    }
}