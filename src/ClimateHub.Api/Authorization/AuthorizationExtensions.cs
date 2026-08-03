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
}