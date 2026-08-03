using System.Security.Claims;
using ClimateHub.Modules.IAM.Contracts;
using ClimateHub.Modules.IAM.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClimateHub.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        g.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.LoginAsync(request, ct);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Problem(statusCode: 401, detail: "Invalid credentials");
            }
        }).AllowAnonymous();

        g.MapPost("/refresh", async (RefreshRequest request, IAuthService authService, CancellationToken ct) =>
        {
            try
            {
                var result = await authService.RefreshTokenAsync(request, ct);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Problem(statusCode: 401, detail: "Invalid or expired refresh token");
            }
        }).AllowAnonymous();

        g.MapPost("/revoke", async (RevokeRequest request, IAuthService authService, HttpContext httpContext, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null)
                return Results.Problem(statusCode: 401, detail: "Not authenticated");

            await authService.RevokeTokenAsync(request.RefreshToken, userId.Value, ct);
            return Results.NoContent();
        }).RequireAuthorization();

        g.MapGet("/me", async (IAuthService authService, HttpContext httpContext, CancellationToken ct) =>
        {
            var userId = GetUserId(httpContext);
            if (userId is null)
                return Results.Problem(statusCode: 401, detail: "Not authenticated");

            try
            {
                var result = await authService.GetCurrentUserAsync(userId.Value, ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.Problem(statusCode: 404, detail: "User not found");
            }
        }).RequireAuthorization();
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim is not null && Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}