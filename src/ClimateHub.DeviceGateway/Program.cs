using ClimateHub.DeviceGateway;
using ClimateHub.DeviceGateway.Services;
using ClimateHub.Infrastructure.Configuration;
using ClimateHub.Modules.Building.Infrastructure;
using ClimateHub.Modules.Commands.Infrastructure;
using ClimateHub.Modules.Devices.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    var postgresConnectionString = builder.Configuration.GetRequiredSection("Postgres:ConnectionString").Value
        ?? throw new InvalidOperationException("Postgres:ConnectionString is required");

    builder.Services.AddClimateHubInfrastructure();
    builder.Services.AddBuildingModule(postgresConnectionString);
    builder.Services.AddDevicesModule(postgresConnectionString);
    builder.Services.AddEnvironmentModule(postgresConnectionString);
    builder.Services.AddCommandsModule(postgresConnectionString);
    builder.Services.AddScoped<TelemetryIngestionHandler>();
    builder.Services.AddScoped<CommandEventConsumer>();
    builder.Services.AddHostedService<DeviceGatewayWorker>();

    var host = builder.Build();

    Log.Information("ClimateHub Device Gateway starting");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ClimateHub Device Gateway terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}