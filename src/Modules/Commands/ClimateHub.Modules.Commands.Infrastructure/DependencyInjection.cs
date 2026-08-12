using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Commands.Application;
using ClimateHub.Modules.Commands.Contracts;
using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using ClimateHub.Modules.Commands.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.Commands.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommandsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CommandsDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "command"))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<ICommandRepository, CommandRepository>();
        services.AddScoped<ICommandOutboxRepository, CommandOutboxRepository>();
        services.AddScoped<ICommandInboxRepository, CommandInboxRepository>();
        services.AddScoped<IDeviceCapabilityStateRepository, DeviceCapabilityStateRepository>();
        services.AddScoped<CapabilityService>();
        services.AddScoped<CreateCommandHandler>();
        services.AddScoped<ICommandsModule, CommandsModuleService>();
        services.AddScoped<CancelCommandHandler>();
        services.AddOptions<CommandTimeoutOptions>()
            .BindConfiguration(CommandTimeoutOptions.SectionName)
            .ValidateDataAnnotations();
        services.AddHostedService<CommandOutboxWorker>();
        services.AddHostedService<CommandTimeoutWorker>();

        return services;
    }
}
