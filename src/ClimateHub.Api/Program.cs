using System.Net.Sockets;
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
using ClimateHub.Infrastructure.Audit;
using ClimateHub.Modules.IAM;
using ClimateHub.Modules.IAM.Domain;
using ClimateHub.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
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
            options.RefreshOnIssuerSigningKeyNotFound = false;
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
    builder.Services.AddScoped<IAuthorizationHandler, ClimateHub.Api.Authorization.BuildingAccessHandler>();
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

    var argsList = args.ToList();

    if (argsList.Contains("--validate-config"))
    {
        var errors = 0;
        var jwtKey = builder.Configuration.GetSection("Jwt").GetValue<string>("SigningKey");
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("SET_VIA_ENVIRONMENT"))
        {
            Log.Error("Jwt:SigningKey is missing or contains placeholder value");
            errors++;
        }

        var pgConn = builder.Configuration.GetRequiredSection("Postgres").GetValue<string>("ConnectionString");
        if (!string.IsNullOrWhiteSpace(pgConn) && !pgConn.Contains("SET_VIA_ENVIRONMENT"))
        {
            try
            {
                using var pgConnObj = new NpgsqlConnection(pgConn);
                await pgConnObj.OpenAsync();
                pgConnObj.Close();
                Log.Information("PostgreSQL connection verified");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PostgreSQL connection failed");
                errors++;
            }
        }

        var mqttHost = builder.Configuration.GetSection("Mqtt").GetValue<string>("Host");
        var mqttPort = builder.Configuration.GetSection("Mqtt").GetValue<int>("Port");
        if (!string.IsNullOrWhiteSpace(mqttHost))
        {
            try
            {
                using var mqttClient = new TcpClient();
                await mqttClient.ConnectAsync(mqttHost, mqttPort);
                Log.Information("MQTT broker reachable at {Host}:{Port}", mqttHost, mqttPort);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "MQTT broker at {Host}:{Port} is not reachable", mqttHost, mqttPort);
                errors++;
            }
        }

        var influxUrl = builder.Configuration.GetSection("InfluxDb").GetValue<string>("Url");
        if (!string.IsNullOrWhiteSpace(influxUrl) && !influxUrl.Contains("SET_VIA_ENVIRONMENT"))
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await httpClient.GetAsync(influxUrl.TrimEnd('/') + "/ping");
                if (response.IsSuccessStatusCode)
                    Log.Information("InfluxDB reachable at {Url}", influxUrl);
                else
                    Log.Warning("InfluxDB returned {Status} at {Url}", response.StatusCode, influxUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "InfluxDB at {Url} is not reachable", influxUrl);
                errors++;
            }
        }

        if (errors > 0)
        {
            Log.Error("Configuration validation failed with {ErrorCount} error(s)", errors);
            return;
        }

        Log.Information("Configuration validation passed");
        return;
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