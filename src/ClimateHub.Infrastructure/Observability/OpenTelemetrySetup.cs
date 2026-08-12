using ClimateHub.SharedKernel.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ClimateHub.Infrastructure.Observability;

public static class OpenTelemetrySetup
{
    public static IServiceCollection AddClimateHubOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(builder => builder
                .AddService(Diagnostics.ServiceName, serviceVersion: Diagnostics.ServiceVersion))
            .WithTracing(tracing => tracing
                .AddSource(Diagnostics.ActivitySource.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter(Diagnostics.Meter.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddProcessInstrumentation()
                .AddRuntimeInstrumentation());

        return services;
    }
}
