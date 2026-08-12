using ClimateHub.Modules.Building.Infrastructure;
using ClimateHub.Modules.Devices.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure;
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

    Log.Information("ClimateHub migrator starting");

    var seed = args.Contains("--seed");
    if (seed)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
        {
            Log.Error("Seeding is only allowed in Development environment (got {Environment})", env);
            return -1;
        }
    }

    using var provider = ClimateHub.Migrator.MigrationRunner.BuildServices(connectionString).BuildServiceProvider();

    foreach (var ctx in ClimateHub.Migrator.MigrationRunner.AllContexts(provider))
    {
        Log.Information("Migrating {DbContext}", ctx.GetType().Name);
        await ctx.Database.MigrateAsync();
        Log.Information("Migration complete for {DbContext}", ctx.GetType().Name);
    }

    Log.Information("All migrations applied successfully");

    if (seed)
    {
        await ClimateHub.Api.Seeding.SeedDevelopmentData.SeedAsync(
            provider.GetRequiredService<BuildingDbContext>(),
            provider.GetRequiredService<DevicesDbContext>(),
            provider.GetRequiredService<EnvironmentDbContext>());
        Log.Information("Development seed data applied");
    }

    Log.Information("Migrator completed successfully");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Migrator terminated with an error");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
