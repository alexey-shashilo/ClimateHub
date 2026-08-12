using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClimateHub.Infrastructure.Configuration;

public static class OptionsValidator
{
    public static IServiceCollection ValidateRequiredOptions(this IServiceCollection services)
    {
        services.AddOptions<SharedKernel.Configuration.MqttOptions>()
            .ValidateDataAnnotations();

        services.AddOptions<SharedKernel.Configuration.PostgresOptions>()
            .ValidateDataAnnotations();

        services.AddOptions<SharedKernel.Configuration.InfluxDbOptions>()
            .ValidateDataAnnotations();

        return services;
    }
}
