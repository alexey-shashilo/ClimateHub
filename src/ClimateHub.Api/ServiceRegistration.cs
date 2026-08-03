using ClimateHub.Api.Endpoints;
using ClimateHub.Modules.Building.Application.Commands;
using ClimateHub.Modules.Building.Application.Queries;
using ClimateHub.Modules.Commands.Application;
using ClimateHub.Modules.Devices.Application.Commands;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.EngineeringSystems.Infrastructure;

namespace ClimateHub.Api;

public static class ServiceRegistration
{
    public static IServiceCollection AddClimateHubApiServices(this IServiceCollection services)
    {
        services.AddHealthChecks();
        services.AddOpenApi();
        services.AddScoped<BuildingOpenApiService>();
        services.AddScoped<CreateBuildingHandler>();
        services.AddScoped<CreateFloorHandler>();
        services.AddScoped<CreateRoomHandler>();
        services.AddScoped<RegisterDeviceHandler>();
        services.AddScoped<AssignDeviceHandler>();
        services.AddScoped<UpdateBuildingHandler>();
        services.AddScoped<DeleteBuildingHandler>();
        services.AddScoped<UpdateFloorHandler>();
        services.AddScoped<DeleteFloorHandler>();
        services.AddScoped<UpdateRoomHandler>();
        services.AddScoped<DeleteRoomHandler>();
        services.AddScoped<UpdateDeviceHandler>();
        services.AddScoped<DeleteDeviceHandler>();
        services.AddScoped<CreateCommandHandler>();
        services.AddScoped<CancelCommandHandler>();
        services.AddScoped<IEngineeringSystemRepository, EngineeringSystemRepository>();
        services.AddScoped<ICommandPlanRepository, CommandPlanRepository>();
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        return services;
    }
}

public static class EndpointMapper
{
    public static WebApplication MapClimateHubEndpoints(this WebApplication app)
    {
        app.MapHealthEndpoints();
        app.MapOpenApiBuildingEndpoints();
        app.MapOpenApiFloorEndpoints();
        app.MapOpenApiRoomEndpoints();
        app.MapOpenApiDeviceEndpoints();
        app.MapOpenApiRoomDeviceEndpoints();
        app.MapOpenApiEnvironmentEndpoints();
        app.MapSseEndpoints();
        app.MapOpenApiPolicyEndpoints();
        app.MapCommandEndpoints();
        app.MapDeviceCommandEndpoints();
        app.MapNeedsEndpoints();
        app.MapEngineeringSystemsEndpoints();
        app.MapClimateEndpoints();
        return app;
    }
}