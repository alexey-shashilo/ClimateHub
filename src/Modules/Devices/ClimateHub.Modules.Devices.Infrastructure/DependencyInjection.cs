using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.Devices.Domain.Repositories;
using ClimateHub.Modules.Devices.Infrastructure.Repositories;
using ClimateHub.Modules.Devices.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.Devices.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDevicesModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DevicesDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "device"))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IDevicesModule, DevicesModuleService>();
        services.AddSingleton<ICapabilityCatalog, CapabilityCatalogService>();

        return services;
    }
}
