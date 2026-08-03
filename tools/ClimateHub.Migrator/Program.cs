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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables();

    var config = builder.Build();
    var connectionString = config.GetRequiredSection("Postgres:ConnectionString").Value
        ?? throw new InvalidOperationException("Postgres:ConnectionString is required");

    var services = new ServiceCollection();
    services.AddDbContext<BuildingDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<DevicesDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<EnvironmentDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<CommandsDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<NeedsDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<EngineeringSystemsDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<ClimateDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<InternalEventsDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<IamDbContext>(o => o.UseNpgsql(connectionString));
    services.AddDbContext<AuditLogDbContext>(o => o.UseNpgsql(connectionString));

    services.AddLogging(lb => lb.AddSerilog());

    var sp = services.BuildServiceProvider();

    var dbContexts = new DbContext[]
    {
        sp.GetRequiredService<BuildingDbContext>(),
        sp.GetRequiredService<DevicesDbContext>(),
        sp.GetRequiredService<EnvironmentDbContext>(),
        sp.GetRequiredService<NeedsDbContext>(),
        sp.GetRequiredService<EngineeringSystemsDbContext>(),
        sp.GetRequiredService<ClimateDbContext>(),
        sp.GetRequiredService<InternalEventsDbContext>(),
        sp.GetRequiredService<IamDbContext>(),
        sp.GetRequiredService<AuditLogDbContext>()
    };

    var seed = args.Contains("--seed");
    var success = true;

    foreach (var ctx in dbContexts)
    {
        try
        {
            Log.Information("Migrating {DbContext}", ctx.GetType().Name);
            await ctx.Database.MigrateAsync();
            Log.Information("Migration complete for {DbContext}", ctx.GetType().Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration failed for {DbContext}", ctx.GetType().Name);
            success = false;
        }
    }

    if (!success)
    {
        Log.Error("One or more migrations failed");
        return;
    }

    Log.Information("All migrations applied successfully");

    if (seed)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (env != "Development")
        {
            Log.Error("Seeding is only allowed in Development environment");
            return;
        }

        await ClimateHub.Api.Seeding.SeedDevelopmentData.SeedAsync(
            sp.GetRequiredService<BuildingDbContext>(),
            sp.GetRequiredService<DevicesDbContext>(),
            sp.GetRequiredService<EnvironmentDbContext>());
        Log.Information("Development seed data applied");
    }

    Log.Information("Migration tool completed successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Migration tool terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}