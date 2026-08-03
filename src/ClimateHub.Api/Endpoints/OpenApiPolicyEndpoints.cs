using ClimateHub.Api.Authorization;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class OpenApiPolicyEndpoints
{
    public static void MapOpenApiPolicyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/rooms/{roomId}/policy", async (string roomId, IRoomPolicyRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var policy = await repo.GetByRoomAsync(RoomId.From(guid), ct);
            if (policy is null)
                return Results.Ok(new
                {
                    temperature = new { minimum = 20.0, maximum = 26.0, preferred = 23.0, controlMode = "monitorOnly" },
                    humidity = new { minimum = 30.0, maximum = 60.0, preferred = 45.0, controlMode = "monitorOnly" },
                    co2 = new { minimum = 0.0, maximum = 1000.0, preferred = 600.0, controlMode = "monitorOnly" },
                    illuminance = new { minimum = 300.0, maximum = 750.0, preferred = 500.0, controlMode = "monitorOnly" }
                });
            return Results.Ok(new
            {
                temperature = new { minimum = policy.TemperatureMin, maximum = policy.TemperatureMax, preferred = policy.TemperaturePreferred, controlMode = policy.TemperatureMode.ToString() },
                humidity = new { minimum = policy.HumidityMin, maximum = policy.HumidityMax, preferred = policy.HumidityPreferred, controlMode = policy.HumidityMode.ToString() },
                co2 = new { minimum = policy.Co2Min, maximum = policy.Co2Max, preferred = policy.Co2Preferred, controlMode = policy.Co2Mode.ToString() },
                illuminance = new { minimum = policy.IlluminanceMin, maximum = policy.IlluminanceMax, preferred = policy.IlluminancePreferred, controlMode = policy.IlluminanceMode.ToString() }
            });
        }).WithTags("Policy").RequirePermission("policy_read");

        app.MapPut("/api/v1/rooms/{roomId}/policy", async (string roomId, HttpContext ctx, IRoomPolicyRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<PolicyUpdateRequest>(cancellationToken: ct);
            if (body is null) return Results.Problem(statusCode: 400, detail: "Invalid body");
            var rid = RoomId.From(guid);
            var existing = await repo.GetByRoomAsync(rid, ct);
            if (existing is null)
            {
                existing = ClimateHub.Modules.Environment.Domain.RoomPolicy.Create(rid,
                    body.Temperature?.Minimum, body.Temperature?.Maximum, body.Temperature?.Preferred, body.Temperature?.ControlMode,
                    body.Humidity?.Minimum, body.Humidity?.Maximum, body.Humidity?.Preferred, body.Humidity?.ControlMode,
                    body.Co2?.Minimum, body.Co2?.Maximum, body.Co2?.Preferred, body.Co2?.ControlMode,
                    body.Illuminance?.Minimum, body.Illuminance?.Maximum, body.Illuminance?.Preferred, body.Illuminance?.ControlMode);
            }
            else
            {
                existing.Update(
                    body.Temperature?.Minimum, body.Temperature?.Maximum, body.Temperature?.Preferred, body.Temperature?.ControlMode,
                    body.Humidity?.Minimum, body.Humidity?.Maximum, body.Humidity?.Preferred, body.Humidity?.ControlMode,
                    body.Co2?.Minimum, body.Co2?.Maximum, body.Co2?.Preferred, body.Co2?.ControlMode,
                    body.Illuminance?.Minimum, body.Illuminance?.Maximum, body.Illuminance?.Preferred, body.Illuminance?.ControlMode);
            }
            await repo.UpsertAsync(existing, ct);
            return Results.NoContent();
        }).WithTags("Policy").RequirePermission("policy_configure");
    }
}

public class PolicyBoundDto
{
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
    public double? Preferred { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("controlMode")] public ClimateHub.Modules.Environment.Domain.PolicyControlMode? ControlMode { get; set; }
}
public class PolicyUpdateRequest { public PolicyBoundDto? Temperature { get; set; } public PolicyBoundDto? Humidity { get; set; } public PolicyBoundDto? Co2 { get; set; } public PolicyBoundDto? Illuminance { get; set; } }