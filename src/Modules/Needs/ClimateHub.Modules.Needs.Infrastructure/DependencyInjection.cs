using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Needs.Contracts;
using ClimateHub.Modules.Needs.Domain;
using ClimateHub.Modules.Needs.Domain.Repositories;
using ClimateHub.Modules.Needs.Infrastructure;
using ClimateHub.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Needs;

public static class DependencyInjection
{
    public static IServiceCollection AddNeedsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NeedsDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "needs")
                      .MigrationsAssembly(typeof(NeedsDbContext).Assembly.FullName!))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));
        services.AddScoped<INeedRepository, NeedsRepository>();
        services.AddScoped<IRoomParameterEvaluationStateRepository, RoomParameterEvaluationStateRepository>();
        services.AddScoped<NeedCalculator>();
        services.AddScoped<NeedEvaluator>();
        services.AddScoped<NeedEvaluationService>();
        services.AddScoped<INeedModule, NeedModuleService>();
        services.AddSingleton<AntiOscillationOptions>(_ => AntiOscillationOptions.Default);
        services.AddScoped<NeedDeviceResolver>();
        services.AddScoped<IRoomBuildingResolver, RoomBuildingResolver>();
        services.AddScoped<CommandTerminalEventHandlers>();
        services.AddScoped<RoomEnvironmentStateChangedHandler>();
        services.AddHostedService<NeedEngineWorker>();
        return services;
    }
}
