using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClimateHub.Infrastructure.Configuration;

public static class OptionsValidator
{
    public static IServiceCollection ValidateRequiredOptions(this IServiceCollection services)
    {
        services.AddOptions<SharedKernel.Configuration.MqttOptions>()
            .BindConfiguration(SharedKernel.Configuration.MqttOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddOptions<SharedKernel.Configuration.PostgresOptions>()
            .BindConfiguration(SharedKernel.Configuration.PostgresOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddOptions<SharedKernel.Configuration.InfluxDbOptions>()
            .BindConfiguration(SharedKernel.Configuration.InfluxDbOptions.SectionName)
            .ValidateDataAnnotations();

        return services;
    }
}
