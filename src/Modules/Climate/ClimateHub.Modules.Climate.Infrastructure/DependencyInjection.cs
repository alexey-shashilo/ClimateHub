using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Climate.Application;
using ClimateHub.Modules.Climate.Application.ClimatePlanning;
using ClimateHub.Modules.Climate.Application.ConflictResolution;
using ClimateHub.Modules.Climate.Application.DependencyGraph;
using ClimateHub.Modules.Climate.Application.EffectModel;
using ClimateHub.Modules.Climate.Application.Events;
using ClimateHub.Modules.Climate.Application.Execution;
using ClimateHub.Modules.Climate.Application.GoalPlanning;
using ClimateHub.Modules.Climate.Application.Prioritization;
using ClimateHub.Modules.Climate.Application.Workers;
using ClimateHub.Modules.Climate.Contracts;
using ClimateHub.Modules.Climate.Domain.Repositories;
using ClimateHub.Modules.Climate.Infrastructure;
using ClimateHub.Modules.Climate.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.Climate;

public static class DependencyInjection
{
    public static IServiceCollection AddClimateModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ClimateDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "climate"))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<IClimateGoalRepository, ClimateGoalRepository>();
        services.AddScoped<IClimatePlanRepository, ClimatePlanRepository>();
        services.AddScoped<IClimateResourceRepository, ClimateResourceRepository>();

        services.AddScoped<GoalPlanner>();
        services.AddScoped<CrossSystemEffectModel>();
        services.AddScoped<PriorityEngine>();
        services.AddScoped<ConflictResolver>();
        services.AddScoped<ClimateExecutionCoordinator>();
        services.AddScoped<ClimatePlanner>();
        services.AddScoped<IClimateModule, ClimateModuleService>();

        services.AddScoped<IClimateEventInboxRepository, ClimateEventInboxRepository>();
        services.AddScoped<ClimateSseProjectionHandler>();
        services.AddScoped<ClimateLifecycleEventPublisher>();
        services.AddScoped<ClimateDependencyGraphValidator>();
        services.AddScoped<ClimatePlanEffectEvaluator>();

        services.Configure<ClimateEventConsumerOptions>(options =>
        {
            options.Enabled = true;
            options.BatchSize = 50;
            options.PollingIntervalMs = 1000;
        });
        services.AddHostedService<ClimateEventConsumerWorker>();

        services.Configure<ClimateReconciliationOptions>(options =>
        {
            options.Enabled = true;
            options.ScanIntervalMs = 15000;
            options.MaximumEffectWaitDuration = TimeSpan.FromMinutes(30);
        });
        services.AddHostedService<ClimateReconciliationWorker>();

        return services;
    }
}