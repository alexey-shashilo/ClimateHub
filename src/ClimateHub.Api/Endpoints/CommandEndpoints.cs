using ClimateHub.Api.Authorization;
using ClimateHub.Modules.Commands.Application;

namespace ClimateHub.Api.Endpoints;

public static class CommandEndpoints
{
    public static void MapCommandEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/commands").WithTags("Commands");

        g.MapPost("/", async (HttpContext ctx, CancellationToken ct) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<CreateCommandRequest>(cancellationToken: ct);
            if (body is null) return Results.Problem(statusCode: 400, detail: "Invalid body");
            var handler = ctx.RequestServices.GetRequiredService<ClimateHub.Modules.Commands.Application.CreateCommandHandler>();
            try
            {
                var cmd = await handler.HandleAsync(body, ct);
                return Results.Created($"/api/v1/commands/{cmd.Id}", new
                {
                    commandId = cmd.Id.ToString(),
                    status = cmd.Status.ToString(),
                    buildingId = cmd.BuildingId.ToString(),
                    roomId = cmd.RoomId?.ToString(),
                    deviceId = cmd.DeviceId.ToString(),
                    capabilityCode = cmd.CapabilityCode,
                    operation = cmd.Operation,
                    parameters = cmd.ParametersJson,
                    createdAt = cmd.CreatedAt,
                    expiresAt = cmd.ExpiresAt,
                    version = cmd.Version
                });
            }
            catch (KeyNotFoundException ex) { return Results.Problem(statusCode: 404, detail: ex.Message); }
            catch (InvalidOperationException ex) { return Results.Problem(statusCode: 400, detail: ex.Message); }
            catch (ArgumentException ex) { return Results.Problem(statusCode: 400, detail: ex.Message); }
        }).RequirePermission("command_create");

        g.MapGet("/", async (ClimateHub.Modules.Commands.Domain.Repositories.ICommandRepository repo, CancellationToken ct) =>
        {
            var cmds = await repo.GetRecentAsync(50, ct);
            return Results.Ok(cmds);
        }).RequirePermission("command_read");

        g.MapGet("/{commandId}", async (string commandId, ClimateHub.Modules.Commands.Domain.Repositories.ICommandRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(commandId, out var guid)) return Results.Problem(statusCode: 400, detail: "COMMAND_NOT_FOUND");
            var cmd = await repo.GetByIdAsync(ClimateHub.Modules.Commands.Domain.CommandId.From(guid), ct);
            return cmd is null ? Results.Problem(statusCode: 404, detail: "COMMAND_NOT_FOUND") : Results.Ok(new
            {
                commandId = cmd.Id.ToString(),
                status = cmd.Status.ToString(),
                buildingId = cmd.BuildingId.ToString(),
                roomId = cmd.RoomId?.ToString(),
                deviceId = cmd.DeviceId.ToString(),
                capabilityCode = cmd.CapabilityCode,
                operation = cmd.Operation,
                parameters = cmd.ParametersJson,
                createdAt = cmd.CreatedAt,
                completedAt = cmd.CompletedAt,
                lastErrorCode = cmd.LastErrorCode
            });
        }).RequireCommandAccess().RequirePermission("command_read");

        g.MapPost("/{commandId}/cancel", async (string commandId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(commandId, out var guid)) return Results.Problem(statusCode: 400, detail: "COMMAND_NOT_FOUND");
            var handler = ctx.RequestServices.GetRequiredService<ClimateHub.Modules.Commands.Application.CancelCommandHandler>();
            try
            {
                var cmd = await handler.HandleAsync(ClimateHub.Modules.Commands.Domain.CommandId.From(guid), ct);
                return Results.Ok(new { commandId = cmd.Id.ToString(), status = cmd.Status.ToString() });
            }
            catch (KeyNotFoundException ex) { return Results.Problem(statusCode: 404, detail: ex.Message); }
            catch (InvalidOperationException ex) { return Results.Problem(statusCode: 400, detail: ex.Message); }
        }).RequireCommandAccess().RequirePermission("command_cancel");
    }

    public static void MapDeviceCommandEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/devices/{deviceId}/commands", async (string deviceId, ClimateHub.Modules.Commands.Domain.Repositories.ICommandRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(deviceId, out var guid)) return Results.Problem(statusCode: 400, detail: "DEVICE_NOT_FOUND");
            var cmds = await repo.GetByDeviceAsync(ClimateHub.SharedKernel.Primitives.DeviceId.From(guid), 50, ct);
            return Results.Ok(cmds);
        }).WithTags("Commands").RequireDeviceAccess().RequirePermission("command_read");

        app.MapGet("/api/v1/rooms/{roomId}/commands", async (string roomId, ClimateHub.Modules.Commands.Domain.Repositories.ICommandRepository repo, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var cmds = await repo.GetByRoomAsync(ClimateHub.SharedKernel.Primitives.RoomId.From(guid), 50, ct);
            return Results.Ok(cmds);
        }).WithTags("Commands").RequireRoomAccess().RequirePermission("command_read");
    }
}
