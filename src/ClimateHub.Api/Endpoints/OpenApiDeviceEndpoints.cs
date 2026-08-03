using ClimateHub.Api.Authorization;
using ClimateHub.Modules.Devices.Application.Commands;
using ClimateHub.Modules.Devices.Application.OpenApi;
using ClimateHub.Modules.Devices.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Dto = ClimateHub.Modules.Devices.Application.OpenApi;

namespace ClimateHub.Api.Endpoints;

public static class OpenApiDeviceEndpoints
{
    public static void MapOpenApiDeviceEndpoints(this WebApplication app)
    {
        var dg = app.MapGroup("/api/v1/devices").WithTags("Devices");

        dg.MapGet("/", async (IDeviceRepository repo, CancellationToken ct) =>
        {
            var devices = await repo.GetAllAsync(ct);
            var dtos = devices.Select(ToDeviceSummary).ToList();
            return Results.Ok(dtos);
        }).RequirePermission("device_read");

        dg.MapGet("/{deviceId}", async (string deviceId, IDeviceRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(deviceId, out var guid)) return Results.Problem(statusCode: 404, detail: "DEVICE_NOT_FOUND");
            var device = await repo.GetByIdAsync(DeviceId.From(guid), ct);
            return device is null ? Results.Problem(statusCode: 404, detail: "DEVICE_NOT_FOUND") : Results.Ok(ToDeviceSummary(device));
        }).RequirePermission("device_read");

        dg.MapPost("/", async (HttpContext ctx, CancellationToken ct) =>
        {
            var command = await ctx.Request.ReadFromJsonAsync<RegisterDeviceCommand>(cancellationToken: ct);
            if (command is null) return Results.Problem(statusCode: 400, detail: "VALIDATION_ERROR");
            var handler = ctx.RequestServices.GetRequiredService<RegisterDeviceHandler>();
            var result = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/devices/{result.Id}", result with { Id = result.Id.ToString() });
        }).RequirePermission("device_register");

        dg.MapPost("/{deviceId}/assignments", async (
            string deviceId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(deviceId, out var guid)) return Results.Problem(statusCode: 404, detail: "DEVICE_NOT_FOUND");
            var request = await ctx.Request.ReadFromJsonAsync<OpenApiAssignRequest>(cancellationToken: ct);
            if (request is null || string.IsNullOrWhiteSpace(request.RoomId)) return Results.Problem(statusCode: 400, detail: "VALIDATION_ERROR");
            var handler = ctx.RequestServices.GetRequiredService<AssignDeviceHandler>();
            if (!Guid.TryParse(request.RoomId, out var roomGuid)) return Results.Problem(statusCode: 400, detail: "VALIDATION_ERROR");
            var cmd = new AssignDeviceCommand { DeviceId = DeviceId.From(guid), RoomId = SharedKernel.Primitives.RoomId.From(roomGuid) };
            var result = await handler.HandleAsync(cmd, ct);
            return Results.Ok(result);
        }).RequirePermission("device_configure");

        dg.MapPut("/{deviceId}", async (string deviceId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(deviceId, out var guid)) return Results.Problem(statusCode: 404, detail: "DEVICE_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<UpdateDeviceRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name)) return Results.Problem(statusCode: 400, detail: "name is required");
            var handler = ctx.RequestServices.GetRequiredService<UpdateDeviceHandler>();
            await handler.HandleAsync(new UpdateDeviceCommand { Id = DeviceId.From(guid), Name = body.Name }, ct);
            return Results.NoContent();
        }).RequirePermission("device_configure");

        dg.MapDelete("/{deviceId}", async (string deviceId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(deviceId, out var guid)) return Results.Problem(statusCode: 404, detail: "DEVICE_NOT_FOUND");
            var handler = ctx.RequestServices.GetRequiredService<DeleteDeviceHandler>();
            await handler.HandleAsync(new DeleteDeviceCommand { Id = DeviceId.From(guid) }, ct);
            return Results.NoContent();
        }).RequirePermission("device_delete");
    }

    public static void MapOpenApiRoomDeviceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/rooms/{roomId}/devices", async (string roomId, IDeviceRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var roomIdObj = SharedKernel.Primitives.RoomId.From(guid);
            var devices = await repo.GetByRoomAsync(roomIdObj, ct);
            var dtos = devices.Select(ToDeviceSummary).ToList();
            return Results.Ok(dtos);
        }).WithTags("Devices").RequirePermission("device_read");
    }

    private static Dto.DeviceSummaryDto ToDeviceSummary(Modules.Devices.Domain.Aggregates.Device d) => new(
        d.Id.ToString(),
        d.HardwareId,
        d.Name,
        $"{d.Model.Manufacturer} {d.Model.ModelName}".Trim(),
        "environmental-sensor",
        d.Status.ToString().ToLowerInvariant(),
        d.ConnectivityState.ToString().ToLowerInvariant(),
        null, d.LastSeenAt,
        d.ActiveAssignment?.RoomId.ToString(),
        null,
        d.Capabilities.Select(c => new Dto.DeviceCapabilityDto(
            c.Code,
            c.DataType is not null ? "measurement" : "system",
            c.Unit, c.MinValue, c.MaxValue,
            false, c.Status == "active")).ToList());
}

public class OpenApiAssignRequest { public string RoomId { get; set; } = ""; }
public class UpdateDeviceRequest { public string Name { get; set; } = ""; }