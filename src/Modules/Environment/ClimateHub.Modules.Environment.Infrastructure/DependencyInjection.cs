using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Environment.Infrastructure.Repositories;
using ClimateHub.Modules.Environment.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.Environment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEnvironmentModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EnvironmentDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "environment"))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<IRoomEnvironmentStateRepository, RoomEnvironmentStateRepository>();
        services.AddScoped<IMessageInboxRepository, MessageInboxRepository>();
        services.AddScoped<ITelemetryOutboxRepository, TelemetryOutboxRepository>();
        services.AddScoped<IInfluxDbWriter, InfluxDbWriter>();
        services.AddScoped<IInfluxDbReader>(sp => (IInfluxDbReader)sp.GetRequiredService<IInfluxDbWriter>());
        services.AddScoped<IEnvironmentModule, EnvironmentModuleService>();
        services.AddScoped<IRoomPolicyRepository, RoomPolicyRepository>();
        services.AddScoped<IRoomEnvironmentStateReader, RoomEnvironmentStateReader>();
        services.AddScoped<IRoomPolicyReader, RoomPolicyReader>();
        services.AddHostedService<TelemetryOutboxWorker>();

        return services;
    }
}
