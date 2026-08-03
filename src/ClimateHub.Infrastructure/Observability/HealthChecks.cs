using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClimateHub.Infrastructure.Observability;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddClimateHubHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Climate Hub is running"), tags: ["live", "ready"]);

        return services;
    }
}