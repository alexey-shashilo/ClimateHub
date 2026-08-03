using ClimateHub.Infrastructure.Observability;
using ClimateHub.Modules.Commands.Contracts;
using ClimateHub.Modules.EngineeringSystems.Application;
using ClimateHub.Modules.EngineeringSystems.Application.Thermal;
using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.EngineeringSystems.Infrastructure;
using ClimateHub.Modules.EngineeringSystems.Infrastructure.Repositories;
using ClimateHub.Modules.Environment.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimateHub.Modules.EngineeringSystems;

public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringSystemsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EngineeringSystemsDbContext>((sp, options) =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "engineering"))
            .AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>()));

        services.AddScoped<IEngineeringSystemRepository, EngineeringSystemRepository>();
        services.AddScoped<ICommandPlanRepository, CommandPlanRepository>();
        services.AddScoped<IStrategyRepository, StrategyRepository>();

        services.AddScoped<CapabilityRouter>();
        services.AddScoped<StrategyEngine>();
        services.AddScoped<ResourceManager>();
        services.AddScoped<EngineeringDeviceSelector>();
        services.AddScoped<CommandPlanExecutor>();
        
        services.AddScoped<IEngineeringSystemsModule, EngineeringSystemsModuleService>();

        services.AddScoped<VentilationDemandFactory>();
        services.AddScoped<VentilationDemandAggregator>();
        services.AddScoped<AirflowBalancePlanner>();
        services.AddScoped<FanOutputMapper>();
        services.AddScoped<DamperPositionMapper>();
        services.AddScoped<HeatRecoveryPlanner>();
        services.AddScoped<SupplyAirTemperaturePlanner>();
        services.AddScoped<FrostProtectionPolicy>();
        services.AddScoped<VentilationControlStrategy>();
        services.AddScoped<HvacSafeStopPlanner>();

        services.AddScoped<IThermalZoneRepository, ThermalZoneRepository>();
        services.AddScoped<IHeatSourceRepository, HeatSourceRepository>();
        services.AddScoped<IHydraulicCircuitRepository, HydraulicCircuitRepository>();
        services.AddScoped<IMixingUnitRepository, MixingUnitRepository>();
        services.AddScoped<ICirculationPumpRepository, CirculationPumpRepository>();
        services.AddScoped<IBufferTankRepository, BufferTankRepository>();
        services.AddScoped<IDomesticHotWaterSystemRepository, DomesticHotWaterSystemRepository>();
        services.AddScoped<IWeatherCompensationCurveRepository, WeatherCompensationCurveRepository>();
        services.AddScoped<IThermalDemandRepository, ThermalDemandRepository>();

        services.AddScoped<WeatherCompensationPlanner>();
        services.AddScoped<HeatLossEstimator>();
        services.AddScoped<ThermalDemandAggregator>();
        services.AddScoped<HeatSourceScheduler>();
        services.AddScoped<HydraulicDemandAllocator>();
        services.AddScoped<BufferTankPlanner>();
        services.AddScoped<DHWPlanner>();
        services.AddScoped<ThermalStrategyEngine>();

        services.Configure<ThermalReconciliationOptions>(o => { o.Enabled = true; o.ScanIntervalMs = 30000; });
        services.AddHostedService<ThermalReconciliationWorker>();

        // Humidification
        services.AddScoped<CondensationProtectionPlanner>();
        services.AddScoped<HumidityCalculationEngine>();
        services.AddScoped<HumidificationDemandAggregator>();
        services.AddScoped<HumidificationStrategyEngine>();

        // Lighting & Shading
        services.AddScoped<SolarPositionCalculator>();
        services.AddScoped<DaylightHarvestingPlanner>();
        services.AddScoped<SolarProtectionPlanner>();
        services.AddScoped<CircadianLightingPlanner>();
        services.AddScoped<OccupancyPlanner>();
        services.AddScoped<LightingStrategyEngine>();

        return services;
    }
}