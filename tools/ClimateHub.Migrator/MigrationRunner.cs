using ClimateHub.Infrastructure.Audit;
using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Modules.Building.Infrastructure;
using ClimateHub.Modules.Climate.Infrastructure;
using ClimateHub.Modules.Commands.Infrastructure;
using ClimateHub.Modules.Devices.Infrastructure;
using ClimateHub.Modules.EngineeringSystems.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure;
using ClimateHub.Modules.IAM.Infrastructure;
using ClimateHub.Modules.Needs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Migrator;

/// <summary>
/// Applies all EF Core migrations for every bounded-context database in ClimateHub.
/// Used by the standalone migrator executable, integration tests, and the E2E harness
/// so schema bootstrap always follows a single, verified path.
/// </summary>
public static class MigrationRunner
{
    /// <summary>
    /// Registers every production DbContext against a single PostgreSQL connection.
    /// </summary>
    public static ServiceCollection BuildServices(string connectionString)
    {
        var services = new ServiceCollection();
        AddContext<BuildingDbContext>(services, connectionString, "building");
        AddContext<DevicesDbContext>(services, connectionString, "device");
        AddContext<EnvironmentDbContext>(services, connectionString, "environment");
        AddContext<CommandsDbContext>(services, connectionString, "command");
        AddContext<NeedsDbContext>(services, connectionString, "needs");
        AddContext<EngineeringSystemsDbContext>(services, connectionString, "engineering");
        AddContext<ClimateDbContext>(services, connectionString, "climate");
        AddContext<InternalEventsDbContext>(services, connectionString, "platform");
        AddContext<IamDbContext>(services, connectionString, "iam");
        AddContext<AuditLogDbContext>(services, connectionString, "audit");
        return services;
    }

    private static void AddContext<TContext>(IServiceCollection services, string connectionString, string schema)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(o =>
            o.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", schema)
                      .MigrationsAssembly(typeof(TContext).Assembly.FullName!)));
    }

    /// <summary>
    /// Resolves every ClimateHub DbContext type in dependency order.
    /// </summary>
    public static DbContext[] ResolveAll(ServiceProvider provider) =>
    [
        provider.GetRequiredService<IamDbContext>(),
        provider.GetRequiredService<BuildingDbContext>(),
        provider.GetRequiredService<DevicesDbContext>(),
        provider.GetRequiredService<EnvironmentDbContext>(),
        provider.GetRequiredService<NeedsDbContext>(),
        provider.GetRequiredService<EngineeringSystemsDbContext>(),
        provider.GetRequiredService<ClimateDbContext>(),
        provider.GetRequiredService<CommandsDbContext>(),
        provider.GetRequiredService<InternalEventsDbContext>(),
        provider.GetRequiredService<AuditLogDbContext>()
    ];

    /// <summary>
    /// Returns all registered ClimateHub DbContext instances from the given provider.
    /// </summary>
    public static IEnumerable<DbContext> AllContexts(ServiceProvider provider)
    {
        yield return provider.GetRequiredService<IamDbContext>();
        yield return provider.GetRequiredService<BuildingDbContext>();
        yield return provider.GetRequiredService<DevicesDbContext>();
        yield return provider.GetRequiredService<EnvironmentDbContext>();
        yield return provider.GetRequiredService<NeedsDbContext>();
        yield return provider.GetRequiredService<EngineeringSystemsDbContext>();
        yield return provider.GetRequiredService<ClimateDbContext>();
        yield return provider.GetRequiredService<CommandsDbContext>();
        yield return provider.GetRequiredService<InternalEventsDbContext>();
        yield return provider.GetRequiredService<AuditLogDbContext>();
    }

    /// <summary>
    /// Applies all migrations for every context against the given connection string.
    /// </summary>
    public static async Task ApplyAllAsync(string connectionString, CancellationToken ct = default)
    {
        using var provider = BuildServices(connectionString).BuildServiceProvider();
        foreach (var ctx in AllContexts(provider))
        {
            await ctx.Database.MigrateAsync(ct);
        }
    }
}
