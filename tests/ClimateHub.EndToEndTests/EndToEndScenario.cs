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
        _http = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task HighCo2_CreatesNeed_AndNormalCo2_SatisfiesIt()
    {
        var (buildingId, floorId, roomId, deviceId) = await SetupBuildingInfrastructure();

        await ConfigureCo2Policy(roomId, "Automatic");

        await PublishHighCo2Telemetry(deviceId, buildingId);

        var need = await WaitForNeedCreated(roomId);
        Assert.NotNull(need);
        Assert.Equal("Co2Reduction", need.Value.GetProperty("type").GetString());

        await PublishNormalCo2Telemetry(deviceId, buildingId);

        var satisfied = await WaitForNeedSatisfied(roomId);
        Assert.NotNull(satisfied);
        Assert.Equal("Satisfied", satisfied.Value.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CommandWithoutImprovement_TimesOut_PlanFailed()
    {
        var (buildingId, floorId, roomId, deviceId) = await SetupBuildingInfrastructure();

        await ConfigureCo2Policy(roomId, "Automatic");

        await PublishHighCo2Telemetry(deviceId, buildingId);

        var need = await WaitForNeedCreated(roomId);
        Assert.NotNull(need);

        await PublishHighCo2Telemetry(deviceId, buildingId);

        var failed = await WaitForNeedBlocked(roomId);
        Assert.NotNull(failed);
        Assert.Equal("Blocked", failed.Value.GetProperty("status").GetString());
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

    private async Task ConfigureCo2Policy(string roomId, string controlMode)
    {
        var setPolicy = await _http.PutAsJsonAsync($"/api/v1/rooms/{roomId}/policy", new
        {
            co2 = new
            {
                minimum = 0.0,
                maximum = 1000.0,
                preferred = 600.0,
                controlMode
            }
        });
        setPolicy.EnsureSuccessStatusCode();
    }

    private async Task PublishHighCo2Telemetry(string deviceId, string buildingId)
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
            payload = new { co2Ppm = 1800 }
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await _fixture.AdminMqttClient.PublishAsync(message);
        Assert.True(result.IsSuccess);
    }

    private async Task PublishNormalCo2Telemetry(string deviceId, string buildingId)
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
            payload = new { co2Ppm = 400 }
        });

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var result = await _fixture.AdminMqttClient.PublishAsync(message);
        Assert.True(result.IsSuccess);
    }

    private async Task<JsonElement?> WaitForNeedCreated(string roomId)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(3);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(2000);
            var resp = await _http.GetAsync($"/api/v1/needs/rooms/{roomId}");
            if (!resp.IsSuccessStatusCode) continue;
            var body = await resp.Content.ReadAsStringAsync();
            try
            {
                var needs = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (needs.ValueKind == System.Text.Json.JsonValueKind.Array && needs.GetArrayLength() > 0)
                    return needs[0];
            }
            catch { }
        }
        return null;
    }

    private async Task<JsonElement?> WaitForNeedSatisfied(string roomId)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(4);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(2000);
            var resp = await _http.GetAsync($"/api/v1/needs/rooms/{roomId}");
            if (!resp.IsSuccessStatusCode) continue;
            var needs = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            if (needs.ValueKind != JsonValueKind.Array || needs.GetArrayLength() == 0) continue;
            var status = needs[0].GetProperty("status").GetString();
            if (status == "Satisfied")
                return needs[0];
        }
        return null;
    }

    private async Task<JsonElement?> WaitForNeedBlocked(string roomId)
    {
        for (int i = 0; i < 30; i++)
        {
            await Task.Delay(1000);
            var resp = await _http.GetAsync($"/api/v1/needs/rooms/{roomId}");
            if (!resp.IsSuccessStatusCode) continue;
            var needs = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            if (needs.ValueKind != JsonValueKind.Array || needs.GetArrayLength() == 0) continue;
            var status = needs[0].GetProperty("status").GetString();
            if (status == "Blocked")
                return needs[0];
        }
        return null;
    }
}
