using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.Modules.IAM.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClimateHub.Api.Authorization;

public static class AuthorizationExtensions
{
    public static RouteHandlerBuilder RequireBuildingAccess(this RouteHandlerBuilder builder, string routeParameterName = "buildingId")
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!context.HttpContext.Request.RouteValues.TryGetValue(routeParameterName, out var buildingIdValue)
                || buildingIdValue is not string buildingIdStr
                || !Guid.TryParse(buildingIdStr, out var buildingId))
            {
                return Results.Problem(statusCode: 400, detail: $"Invalid or missing {routeParameterName}");
            }

            var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Problem(statusCode: 401, detail: "Not authenticated");
            }

            var grantRepo = context.HttpContext.RequestServices
                .GetRequiredService<IBuildingAccessGrantRepository>();

            var hasAccess = await grantRepo.HasAccessAsync(userId, buildingId);
            if (!hasAccess)
                return Results.Problem(statusCode: 403, detail: "Access denied to this building");

            return await next(context);
        });
    }

    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.RequireAuthorization(policy => policy.RequireAssertion(context =>
        {
            var permissions = context.User.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value);

            return permissions.Contains(permission) || permissions.Contains("admin");
        }));
    }

    public static RouteHandlerBuilder RequireRoomAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!TryGetRouteId(context, "roomId", out var roomId))
                return Results.Problem(statusCode: 400, detail: "Invalid or missing roomId");

            var buildingId = await ResolveBuildingForRoom(context, roomId);
            if (buildingId is null)
                return Results.Problem(statusCode: 404, detail: "Room not found");

            return await CheckBuildingAccessAndProceed(context, next, buildingId.Value);
        });
    }

    public static RouteHandlerBuilder RequireDeviceAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!TryGetRouteId(context, "deviceId", out var deviceId))
                return Results.Problem(statusCode: 400, detail: "Invalid or missing deviceId");

            var buildingId = await ResolveBuildingForDevice(context, deviceId);
            if (buildingId is null)
                return Results.Problem(statusCode: 404, detail: "Device not found");

            return await CheckBuildingAccessAndProceed(context, next, buildingId.Value);
        });
    }

    public static RouteHandlerBuilder RequireNeedAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!TryGetRouteId(context, "needId", out var needId))
                return Results.Problem(statusCode: 400, detail: "Invalid or missing needId");

            var buildingId = await ResolveBuildingForNeed(context, needId);
            if (buildingId is null)
                return Results.Problem(statusCode: 404, detail: "Need not found");

            return await CheckBuildingAccessAndProceed(context, next, buildingId.Value);
        });
    }

    public static RouteHandlerBuilder RequireCommandAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!TryGetRouteId(context, "commandId", out var commandId))
                return Results.Problem(statusCode: 400, detail: "Invalid or missing commandId");

            var buildingId = await ResolveBuildingForCommand(context, commandId);
            if (buildingId is null)
                return Results.Problem(statusCode: 404, detail: "Command not found");

            return await CheckBuildingAccessAndProceed(context, next, buildingId.Value);
        });
    }

    public static RouteHandlerBuilder RequireFloorAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!TryGetRouteId(context, "floorId", out var floorId))
                return Results.Problem(statusCode: 400, detail: "Invalid or missing floorId");

            var buildingId = await ResolveBuildingForFloor(context, floorId);
            if (buildingId is null)
                return Results.Problem(statusCode: 404, detail: "Floor not found");

            return await CheckBuildingAccessAndProceed(context, next, buildingId.Value);
        });
    }

    public static RouteHandlerBuilder RequireEngineeringSystemAccess(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            if (!TryGetRouteId(context, "id", out var id))
                return Results.Problem(statusCode: 400, detail: "Invalid or missing id");

            var buildingId = await ResolveBuildingForEngineeringSystem(context, id);
            if (buildingId is null)
                return Results.Problem(statusCode: 404, detail: "Engineering system not found");

            return await CheckBuildingAccessAndProceed(context, next, buildingId.Value);
        });
    }

    private static bool TryGetRouteId(EndpointFilterInvocationContext context, string key, out Guid id)
    {
        id = Guid.Empty;
        if (!context.HttpContext.Request.RouteValues.TryGetValue(key, out var value)
            || value is not string str
            || !Guid.TryParse(str, out id))
            return false;
        return true;
    }

    private static async Task<Guid?> ResolveBuildingForRoom(EndpointFilterInvocationContext context, Guid roomId)
    {
        var roomRepo = context.HttpContext.RequestServices.GetRequiredService<IRoomRepository>();
        var room = await roomRepo.GetByIdAsync(new SharedKernel.Primitives.RoomId(roomId));
        return room?.BuildingId.Value;
    }

    private static async Task<Guid?> ResolveBuildingForDevice(EndpointFilterInvocationContext context, Guid deviceId)
    {
        var deviceRepo = context.HttpContext.RequestServices.GetRequiredService<ClimateHub.Modules.Devices.Domain.Repositories.IDeviceRepository>();
        var device = await deviceRepo.GetByIdAsync(SharedKernel.Primitives.DeviceId.From(deviceId));
        if (device?.ActiveAssignment is null)
            return null;
        return await ResolveBuildingForRoom(context, device.ActiveAssignment.RoomId.Value);
    }

    private static async Task<Guid?> ResolveBuildingForNeed(EndpointFilterInvocationContext context, Guid needId)
    {
        var needRepo = context.HttpContext.RequestServices.GetRequiredService<ClimateHub.Modules.Needs.Domain.Repositories.INeedRepository>();
        var need = await needRepo.GetByIdAsync(new ClimateHub.Modules.Needs.Domain.NeedId(needId));
        return need?.BuildingId.Value;
    }

    private static async Task<Guid?> ResolveBuildingForCommand(EndpointFilterInvocationContext context, Guid commandId)
    {
        var cmdRepo = context.HttpContext.RequestServices.GetRequiredService<ClimateHub.Modules.Commands.Domain.Repositories.ICommandRepository>();
        var cmd = await cmdRepo.GetByIdAsync(ClimateHub.Modules.Commands.Domain.CommandId.From(commandId));
        return cmd?.BuildingId.Value;
    }

    private static async Task<Guid?> ResolveBuildingForFloor(EndpointFilterInvocationContext context, Guid floorId)
    {
        var floorRepo = context.HttpContext.RequestServices.GetRequiredService<ClimateHub.Modules.Building.Domain.Repositories.IFloorRepository>();
        var floor = await floorRepo.GetByIdAsync(new ClimateHub.SharedKernel.Primitives.FloorId(floorId));
        return floor?.BuildingId.Value;
    }

    private static async Task<Guid?> ResolveBuildingForEngineeringSystem(EndpointFilterInvocationContext context, Guid id)
    {
        var engRepo = context.HttpContext.RequestServices.GetRequiredService<ClimateHub.Modules.EngineeringSystems.Domain.Repositories.IEngineeringSystemRepository>();
        var system = await engRepo.GetByIdAsync(ClimateHub.Modules.EngineeringSystems.Domain.EngineeringSystemId.From(id));
        return system?.BuildingId.Value;
    }

    private static async ValueTask<IResult?> CheckBuildingAccessAndProceed(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        Guid buildingId)
    {
        var userIdClaim = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Results.Problem(statusCode: 401, detail: "Not authenticated");

        var grantRepo = context.HttpContext.RequestServices
            .GetRequiredService<IBuildingAccessGrantRepository>();

        var hasAccess = await grantRepo.HasAccessAsync(userId, buildingId);
        if (!hasAccess)
            return Results.Problem(statusCode: 403, detail: "Access denied to this building");

        return (IResult?)await next(context);
    }
}