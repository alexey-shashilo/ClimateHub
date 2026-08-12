using ClimateHub.Infrastructure.Audit;
using ClimateHub.Infrastructure.Events;
using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Infrastructure.Observability;
using ClimateHub.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClimateHub.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClimateHubInfrastructure(this IServiceCollection services)
    {
        services
            .ValidateRequiredOptions()
            .AddClimateHubOpenTelemetry()
            .AddClimateHubHealthChecks()
            .AddSingleton<EnvironmentEventBus>()
            .AddScoped<IDomainEventDispatcher, DomainEventDispatcher>()
            .AddScoped<DomainEventInterceptor>()
            .AddSingleton<AppendOnlySaveChangesInterceptor>();

        return services;
    }

    public static IServiceCollection AddInternalEventInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<InternalEventsDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "platform")
                      .MigrationsAssembly(typeof(InternalEventsDbContext).Assembly.FullName!))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<InternalEventOutboxRepository>();
        services.AddScoped<InternalEventInboxRepository>();
        services.AddScoped<SseEventLogRepository>();
        services.AddScoped<RoomEvaluationLock>();
        services.AddScoped<InternalEventOutboxRepository>();
        services.AddScoped<InternalEventInboxRepository>();
        services.AddScoped<SseEventLogRepository>();
        services.AddSingleton<InternalEventDispatcher>();

        services.AddOptions<InternalEventsOptions>()
            .BindConfiguration(InternalEventsOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddHostedService<InternalEventOutboxWorker>();

        return services;
    }

    public static IServiceCollection AddAuditInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AuditLogDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "audit")
                      .MigrationsAssembly(typeof(AuditLogDbContext).Assembly.FullName!))
            .AddInterceptors(sp.GetRequiredService<AppendOnlySaveChangesInterceptor>()));

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<AuditService>();

        return services;
    }
}
