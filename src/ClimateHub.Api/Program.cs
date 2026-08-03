using System.Text;
using ClimateHub.Api;
using ClimateHub.Api.Middleware;
using ClimateHub.Infrastructure.Configuration;
using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Modules.Building.Infrastructure;
using ClimateHub.Modules.Devices.Infrastructure;
using ClimateHub.Modules.Commands.Infrastructure;
using ClimateHub.Modules.Environment.Infrastructure;
using ClimateHub.Modules.Needs;
using ClimateHub.Modules.Needs.Infrastructure;
using ClimateHub.Modules.Climate;
using ClimateHub.Modules.Climate.Infrastructure;
using ClimateHub.Modules.EngineeringSystems;
using ClimateHub.Modules.EngineeringSystems.Infrastructure;
using ClimateHub.Modules.IAM;
using ClimateHub.Modules.IAM.Domain;
using ClimateHub.Api.Authorization;
using ClimateHub.Infrastructure.Audit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

    var postgresConnectionString = builder.Configuration.GetRequiredSection("Postgres:ConnectionString").Value
        ?? throw new InvalidOperationException("Postgres:ConnectionString is required");

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtSigningKey = jwtSection.GetValue<string>("SigningKey")
        ?? throw new InvalidOperationException("Jwt:SigningKey is required");

    builder.Services.AddCors();
    builder.Services.AddClimateHubInfrastructure();
    builder.Services.AddInternalEventInfrastructure(postgresConnectionString);
    builder.Services.AddClimateHubApiServices();
    builder.Services.AddBuildingModule(postgresConnectionString);
    builder.Services.AddDevicesModule(postgresConnectionString);
    builder.Services.AddEnvironmentModule(postgresConnectionString);
    builder.Services.AddCommandsModule(postgresConnectionString);
    builder.Services.AddNeedsModule(postgresConnectionString);
    builder.Services.AddClimateModule(postgresConnectionString);
    builder.Services.AddEngineeringSystemsModule(postgresConnectionString);
    builder.Services.AddIamModule(postgresConnectionString);
    builder.Services.AddAuditInfrastructure(postgresConnectionString);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection.GetValue<string>("Issuer") ?? "ClimateHub",
                ValidAudience = jwtSection.GetValue<string>("Audience") ?? "ClimateHub.Api",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddSingleton<IAuthorizationHandler, ClimateHub.Api.Authorization.BuildingAccessHandler>();
    builder.Services.AddSingleton<IAuthorizationHandler, ClimateHub.Api.Authorization.PermissionHandler>();

    var app = builder.Build();

    // Security middleware (order matters: rate limiting before everything)
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
    app.UseCors(policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader());

    app.UseAuthentication();
    app.UseSerilogRequestLogging();
    app.UseAuthorization();
    app.UseMiddleware<AuditMiddleware>();
    app.UseClimateHubProblemDetails();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Apply pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        try
        {
            await sp.GetRequiredService<BuildingDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<DevicesDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<EnvironmentDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<NeedsDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<EngineeringSystemsDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<ClimateDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<InternalEventsDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<ClimateHub.Modules.IAM.Infrastructure.IamDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<AuditLogDbContext>().Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database migration failed (may be acceptable in dev)");
        }

        try
        {
            await ClimateHub.Api.Seeding.SeedDevelopmentData.SeedAsync(
                sp.GetRequiredService<BuildingDbContext>(),
                sp.GetRequiredService<DevicesDbContext>(),
                sp.GetRequiredService<EnvironmentDbContext>());
            Log.Information("Development seed data applied");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Seed data failed (may be acceptable)");
        }
    }

    app.MapClimateHubEndpoints();

    Log.Information("ClimateHub API starting");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ClimateHub API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }