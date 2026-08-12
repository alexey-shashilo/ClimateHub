using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Building.Contracts;
using ClimateHub.Modules.Building.Domain.Repositories;
using ClimateHub.Modules.Building.Infrastructure.Repositories;
using ClimateHub.Modules.Building.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.Building.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BuildingDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "building"))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<IBuildingRepository, BuildingRepository>();
        services.AddScoped<IFloorRepository, FloorRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBuildingModule, BuildingModuleService>();

        return services;
    }
}
