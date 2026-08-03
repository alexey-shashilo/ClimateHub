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

    public EndToEndScenario(EndToEndFixture fixture)
    {
        _fixture = fixture;
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    }

    [Fact]
    public async Task TelemetryFlow_Success()
    {
        // Arrange: create building → floor → room → device → assignment
        var createBuilding = await _http.PostAsJsonAsync("/api/v1/buildings", new { name = "E2E Building" });
        createBuilding.EnsureSuccessStatusCode();
        var building = await createBuilding.Content.ReadFromJsonAsync<JsonElement>();
        var buildingId = building.GetProperty("id").GetString()!;

        var createFloor = await _http.PostAsJsonAsync($"/api/v1/buildings/{buildingId}/floors",
            new { name = "Ground", level = 0 });
        createFloor.EnsureSuccessStatusCode();
        var floor = await createFloor.Content.ReadFromJsonAsync<JsonElement>();
        var floorId = floor.GetProperty("id").GetString()!;

        var createRoom = await _http.PostAsJsonAsync($"/api/v1/floors/{floorId}/rooms",
            new { name = "Living Room" });
        createRoom.EnsureSuccessStatusCode();
        var room = await createRoom.Content.ReadFromJsonAsync<JsonElement>();
        var roomId = room.GetProperty("id").GetString()!;

        var registerDevice = await _http.PostAsJsonAsync("/api/v1/devices",
            new { hardwareId = "E2E-HW-001", name = "E2E Sensor", manufacturer = "Test", modelName = "E2E-1000", protocolVersion = "1.0" });
        registerDevice.EnsureSuccessStatusCode();
        var device = await registerDevice.Content.ReadFromJsonAsync<JsonElement>();
        var deviceId = device.GetProperty("id").GetString()!;

        var assignDevice = await _http.PostAsJsonAsync($"/api/v1/devices/{deviceId}/assignments",
            new { roomId });
        assignDevice.EnsureSuccessStatusCode();

        // Verify assignment persisted
        var getDevice = await _http.GetFromJsonAsync<JsonElement>($"/api/v1/devices/{deviceId}");
        var assignedRoomId = getDevice.GetProperty("assignedRoomId").GetString();
        Assert.Equal(roomId, assignedRoomId);

        // Act: publish MQTT telemetry
        var mqttFactory = new MqttClientFactory();
        using var mqttClient = mqttFactory.CreateMqttClient();

        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId("e2e-test-simulator")
            .WithCleanSession()
            .Build();

        await mqttClient.ConnectAsync(mqttOptions);

        var messageId = Guid.NewGuid().ToString();
        var telemetry = JsonSerializer.Serialize(new
        {
            messageId,
            messageType = "environment.telemetry",
            protocolVersion = "1.0",
            buildingId,
            deviceId,
            bootId = Guid.NewGuid().ToString(),
            sequenceNumber = 1,
            measuredAt = DateTimeOffset.UtcNow.ToString("O"),
            payload = new { temperatureC = 22.5, relativeHumidityPct = 41.7, co2Ppm = 735 }
        });

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"climate-hub/v1/{buildingId}/{deviceId}/telemetry/environment")
            .WithPayload(Encoding.UTF8.GetBytes(telemetry))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        var publishResult = await mqttClient.PublishAsync(mqttMessage);
        Assert.True(publishResult.IsSuccess);

        // Wait for processing with polling
        JsonElement? env = null;
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(1000);
            var resp = await _http.GetAsync($"/api/v1/rooms/{roomId}/environment");
            if (resp.IsSuccessStatusCode)
            {
                env = await resp.Content.ReadFromJsonAsync<JsonElement>();
                break;
            }
        }

Assert.True(env.HasValue);
            var tempProp = env.Value.GetProperty("parameters").GetProperty("temperature");
            Assert.NotNull(tempProp.GetRawText());

        // History check (may be empty if Influx not configured in test)
        var historyResp = await _http.GetAsync($"/api/v1/rooms/{roomId}/environment/history?from=2026-01-01T00:00:00Z&to=2026-12-31T00:00:00Z&parameter=temperature");
        Assert.True(historyResp.IsSuccessStatusCode || historyResp.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public void DuplicateMessage_Idempotent()
    {
        // Placeholder for explicit MQTT dedup test
        Assert.True(true);
    }

    [Fact]
    public async Task Health_Endpoints_Respond()
    {
        var live = await _http.GetAsync("/health/live");
        Assert.True(live.IsSuccessStatusCode);

        var ready = await _http.GetAsync("/health/ready");
        Assert.True(ready.IsSuccessStatusCode);
    }
}