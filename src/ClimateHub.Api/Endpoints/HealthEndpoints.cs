using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClimateHub.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", service = "ClimateHub.Api" }))
            .WithTags("Health")
            .AllowAnonymous();

        app.MapGet("/health/ready", async (HttpContext context) =>
        {
            var healthCheckService = context.RequestServices.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync(context.RequestAborted);
            return report.Status == HealthStatus.Healthy
                ? Results.Ok(new { status = "healthy", service = "ClimateHub.Api" })
                : Results.Json(new { status = "unhealthy", service = "ClimateHub.Api" }, statusCode: 503);
        })
        .WithTags("Health")
        .AllowAnonymous();
    }
}