using ClimateHub.Api.Authorization;
using ClimateHub.Modules.Building.Application.Commands;
using ClimateHub.Modules.Building.Application.Queries;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Api.Endpoints;

public static class OpenApiBuildingEndpoints
{
    public static void MapOpenApiBuildingEndpoints(this WebApplication app)
    {
        var bg = app.MapGroup("/api/v1/buildings").WithTags("Buildings");

        bg.MapGet("/", async (BuildingOpenApiService service, CancellationToken ct) =>
        {
            var result = await service.ListBuildingsAsync(ct);
            return Results.Ok(result);
        }).RequirePermission("building_read");

        bg.MapPost("/", async (HttpContext ctx, CancellationToken ct) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<CreateBuildingRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name))
                return Results.Problem(statusCode: 400, detail: "VALIDATION_ERROR", title: "name is required");
            var handler = ctx.RequestServices.GetRequiredService<CreateBuildingHandler>();
            var cmd = new CreateBuildingCommand { Name = body.Name, Address = body.Address };
            var result = await handler.HandleAsync(cmd, ct);
            return Results.Created($"/api/v1/buildings/{result.Id}", result);
        }).RequirePermission("building_configure");

        bg.MapGet("/{buildingId}", async (string buildingId, BuildingOpenApiService service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out var guid)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var result = await service.GetBuildingSummaryAsync(BuildingId.From(guid), ct);
            return result is null ? Results.Problem(statusCode: 404, detail: "BUILDING_NOT_FOUND") : Results.Ok(result);
        }).RequireBuildingAccess();

        bg.MapPost("/{buildingId}/floors", async (
            string buildingId,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out var guid)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<CreateFloorRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name))
                return Results.Problem(statusCode: 400, detail: "VALIDATION_ERROR", title: "name is required");
            var floorHandler = ctx.RequestServices.GetRequiredService<CreateFloorHandler>();
            var cmd = new CreateFloorCommand { BuildingId = BuildingId.From(guid), Name = body.Name, Level = body.Level };
            var result = await floorHandler.HandleAsync(cmd, ct);
            return Results.Created($"/api/v1/buildings/{buildingId}/floors/{result.Id}", result);
        }).RequireBuildingAccess().RequirePermission("building_configure");

        bg.MapGet("/{buildingId}/floors", async (string buildingId, BuildingOpenApiService service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out var guid)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var result = await service.GetFloorsAsync(BuildingId.From(guid), ct);
            return Results.Ok(result);
        }).RequireBuildingAccess();

        bg.MapPut("/{buildingId}", async (string buildingId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out var guid)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<UpdateBuildingRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name)) return Results.Problem(statusCode: 400, detail: "name is required");
            var handler = ctx.RequestServices.GetRequiredService<UpdateBuildingHandler>();
            await handler.HandleAsync(new UpdateBuildingCommand { Id = BuildingId.From(guid), Name = body.Name }, ct);
            return Results.NoContent();
        }).RequireBuildingAccess().RequirePermission("building_configure");

        bg.MapDelete("/{buildingId}", async (string buildingId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(buildingId, out var guid)) return Results.Problem(statusCode: 400, detail: "BUILDING_NOT_FOUND");
            var handler = ctx.RequestServices.GetRequiredService<DeleteBuildingHandler>();
            await handler.HandleAsync(new DeleteBuildingCommand { Id = BuildingId.From(guid) }, ct);
            return Results.NoContent();
        }).RequireBuildingAccess().RequirePermission("building_configure");
    }

    public static void MapOpenApiFloorEndpoints(this WebApplication app)
    {
        var fg = app.MapGroup("/api/v1/floors").WithTags("Floors");

        fg.MapPost("/{floorId}/rooms", async (
            string floorId,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(floorId, out var guid)) return Results.Problem(statusCode: 400, detail: "FLOOR_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<CreateRoomRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name))
                return Results.Problem(statusCode: 400, detail: "VALIDATION_ERROR", title: "name is required");
            var roomHandler = ctx.RequestServices.GetRequiredService<CreateRoomHandler>();
            var cmd = new CreateRoomCommand { FloorId = FloorId.From(guid), Name = body.Name, Purpose = body.Purpose };
            var result = await roomHandler.HandleAsync(cmd, ct);
            return Results.Created($"/api/v1/floors/{floorId}/rooms/{result.Id}", result);
        }).RequireFloorAccess().RequirePermission("building_configure");

        fg.MapGet("/{floorId}/rooms", (string floorId, BuildingOpenApiService service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(floorId, out var guid)) return Results.Problem(statusCode: 400, detail: "FLOOR_NOT_FOUND");
            return Results.Ok(Array.Empty<object>());
        }).RequireFloorAccess().RequirePermission("building_read");

        fg.MapPut("/{floorId}", async (string floorId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(floorId, out var guid)) return Results.Problem(statusCode: 400, detail: "FLOOR_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<UpdateFloorRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name)) return Results.Problem(statusCode: 400, detail: "name is required");
            var handler = ctx.RequestServices.GetRequiredService<UpdateFloorHandler>();
            await handler.HandleAsync(new UpdateFloorCommand { Id = FloorId.From(guid), Name = body.Name, Level = body.Level }, ct);
            return Results.NoContent();
        }).RequireFloorAccess().RequirePermission("building_configure");

        fg.MapDelete("/{floorId}", async (string floorId, string? buildingId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(floorId, out var fgid) || string.IsNullOrWhiteSpace(buildingId) || !Guid.TryParse(buildingId, out var bgid))
                return Results.Problem(statusCode: 400, detail: "buildingId query parameter required");
            var handler = ctx.RequestServices.GetRequiredService<DeleteFloorHandler>();
            await handler.HandleAsync(new DeleteFloorCommand { BuildingId = BuildingId.From(bgid), FloorId = FloorId.From(fgid) }, ct);
            return Results.NoContent();
        }).RequireBuildingAccess().RequirePermission("building_configure");
    }

    public static void MapOpenApiRoomEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/rooms/{roomId}", async (string roomId, BuildingOpenApiService service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var result = await service.GetRoomSummaryAsync(RoomId.From(guid), ct);
            return result is null ? Results.Problem(statusCode: 404, detail: "ROOM_NOT_FOUND") : Results.Ok(result);
        }).WithTags("Rooms").RequireRoomAccess().RequirePermission("room_read");

        app.MapPut("/api/v1/rooms/{roomId}", async (string roomId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var guid)) return Results.Problem(statusCode: 400, detail: "ROOM_NOT_FOUND");
            var body = await ctx.Request.ReadFromJsonAsync<UpdateRoomRequest>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Name)) return Results.Problem(statusCode: 400, detail: "name is required");
            var handler = ctx.RequestServices.GetRequiredService<UpdateRoomHandler>();
            await handler.HandleAsync(new UpdateRoomCommand { Id = RoomId.From(guid), Name = body.Name, Purpose = body.Purpose }, ct);
            return Results.NoContent();
        }).WithTags("Rooms").RequireRoomAccess().RequirePermission("room_configure");

        app.MapDelete("/api/v1/rooms/{roomId}", async (string roomId, string? floorId, HttpContext ctx, CancellationToken ct) =>
        {
            if (!Guid.TryParse(roomId, out var rgid) || string.IsNullOrWhiteSpace(floorId) || !Guid.TryParse(floorId, out var fgid))
                return Results.Problem(statusCode: 400, detail: "floorId query parameter required");
            var handler = ctx.RequestServices.GetRequiredService<DeleteRoomHandler>();
            await handler.HandleAsync(new DeleteRoomCommand { FloorId = FloorId.From(fgid), RoomId = RoomId.From(rgid) }, ct);
            return Results.NoContent();
        }).WithTags("Rooms").RequireRoomAccess().RequirePermission("room_configure");
    }
}

public class CreateBuildingRequest { public string Name { get; set; } = ""; public string? Address { get; set; } }
public class CreateFloorRequest { public string Name { get; set; } = ""; public int Level { get; set; } }
public class CreateRoomRequest { public string Name { get; set; } = ""; public string? Purpose { get; set; } }
public class UpdateBuildingRequest { public string Name { get; set; } = ""; public string? Address { get; set; } }
public class UpdateFloorRequest { public string Name { get; set; } = ""; public int Level { get; set; } }
public class UpdateRoomRequest { public string Name { get; set; } = ""; public string? Purpose { get; set; } }
