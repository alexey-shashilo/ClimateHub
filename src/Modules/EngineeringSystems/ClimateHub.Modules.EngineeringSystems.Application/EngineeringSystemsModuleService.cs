using ClimateHub.Modules.EngineeringSystems.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Humidification;
using ClimateHub.Modules.EngineeringSystems.Domain.Lighting;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class EngineeringSystemsModuleService : IEngineeringSystemsModule
{
    private readonly IEngineeringSystemRepository _systemRepo;
    private readonly ICommandPlanRepository _planRepo;
    private readonly CapabilityRouter _router;
    private readonly StrategyEngine _strategyEngine;
    private readonly ResourceManager _resourceManager;
    private readonly CommandPlanExecutor _executor;
    private readonly IRoomEnvironmentStateReader _envReader;
    private readonly VentilationControlStrategy _ventilationStrategy;
    private readonly VentilationDemandFactory _demandFactory;
    private readonly WeatherCompensationPlanner _weatherPlanner;
    private readonly HeatLossEstimator _heatLossEstimator;
    private readonly ThermalStrategyEngine _thermalStrategy;
    private readonly CondensationProtectionPlanner _condensationPlanner;
    private readonly HumidityCalculationEngine _humidityCalc;
    private readonly HumidificationDemandAggregator _humidityAggregator;
    private readonly HumidificationStrategyEngine _humidificationStrategy;
    private readonly SolarPositionCalculator _solarCalc;
    private readonly DaylightHarvestingPlanner _daylightPlanner;
    private readonly SolarProtectionPlanner _solarProtection;
    private readonly CircadianLightingPlanner _circadianPlanner;
    private readonly OccupancyPlanner _occupancyPlanner;
    private readonly LightingStrategyEngine _lightingStrategy;

    public EngineeringSystemsModuleService(
        IEngineeringSystemRepository systemRepo,
        ICommandPlanRepository planRepo,
        CapabilityRouter router,
        StrategyEngine strategyEngine,
        ResourceManager resourceManager,
        CommandPlanExecutor executor,
        IRoomEnvironmentStateReader envReader,
        VentilationControlStrategy ventilationStrategy,
        VentilationDemandFactory demandFactory,
        WeatherCompensationPlanner weatherPlanner,
        HeatLossEstimator heatLossEstimator,
        ThermalStrategyEngine thermalStrategy,
        CondensationProtectionPlanner condensationPlanner,
        HumidityCalculationEngine humidityCalc,
        HumidificationDemandAggregator humidityAggregator,
        HumidificationStrategyEngine humidificationStrategy,
        SolarPositionCalculator solarCalc,
        DaylightHarvestingPlanner daylightPlanner,
        SolarProtectionPlanner solarProtection,
        CircadianLightingPlanner circadianPlanner,
        OccupancyPlanner occupancyPlanner,
        LightingStrategyEngine lightingStrategy)
    {
        _systemRepo = systemRepo; _planRepo = planRepo;
        _router = router; _strategyEngine = strategyEngine;
        _resourceManager = resourceManager; _executor = executor;
        _envReader = envReader;
        _ventilationStrategy = ventilationStrategy;
        _demandFactory = demandFactory;
        _weatherPlanner = weatherPlanner;
        _heatLossEstimator = heatLossEstimator;
        _thermalStrategy = thermalStrategy;
        _condensationPlanner = condensationPlanner;
        _humidityCalc = humidityCalc;
        _humidityAggregator = humidityAggregator;
        _humidificationStrategy = humidificationStrategy;
        _solarCalc = solarCalc;
        _daylightPlanner = daylightPlanner;
        _solarProtection = solarProtection;
        _circadianPlanner = circadianPlanner;
        _occupancyPlanner = occupancyPlanner;
        _lightingStrategy = lightingStrategy;
    }

    public async Task<CapabilityPlanResultDto> PlanAsync(
        EngineeringCapabilityRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var routing = await _router.RouteAsync(request.RoomId, request.CapabilityCode, cancellationToken);
        if (!routing.IsSuccess)
            return new CapabilityPlanResultDto(null, false, routing.FailureCode, routing.FailureReason);

        var system = routing.System!;

        if (request.CapabilityCode == EngineeringCapabilityCodes.ReduceCo2 ||
            request.CapabilityCode == EngineeringCapabilityCodes.IncreaseAirFlow)
        {
            return await PlanVentilationAsync(request, system, cancellationToken);
        }

        if (request.CapabilityCode == EngineeringCapabilityCodes.IncreaseTemperature ||
            request.CapabilityCode == EngineeringCapabilityCodes.DecreaseTemperature)
        {
            return await PlanThermalAsync(request, system, cancellationToken);
        }

        if (request.CapabilityCode == EngineeringCapabilityCodes.IncreaseHumidity ||
            request.CapabilityCode == EngineeringCapabilityCodes.IncreaseHumidityPrecise ||
            request.CapabilityCode == EngineeringCapabilityCodes.IncreaseHumiditySteam ||
            request.CapabilityCode == EngineeringCapabilityCodes.IncreaseHumidityAdiabatic)
        {
            return await PlanHumidificationAsync(request, system, cancellationToken);
        }

        if (request.CapabilityCode == EngineeringCapabilityCodes.IncreaseIlluminance ||
            request.CapabilityCode == EngineeringCapabilityCodes.DecreaseIlluminance ||
            request.CapabilityCode == EngineeringCapabilityCodes.BlindPosition ||
            request.CapabilityCode == EngineeringCapabilityCodes.LightingScene)
        {
            return await PlanLightingAsync(request, system, cancellationToken);
        }

        return await PlanStandardAsync(request, system, routing, cancellationToken);
    }

    private async Task<CapabilityPlanResultDto> PlanVentilationAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.VentilationConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.VentilationConfigurationInvalid,
                "Ventilation system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");
        var currentCo2 = co2Param?.Value ?? request.CurrentValue ?? 800;
        var targetCo2 = request.TargetValue ?? 1000;

        var outdoorTemp = 5.0;
        var outdoorParam = parameters.FirstOrDefault(p => p.Parameter == "outdoor_temperature");
        if (outdoorParam?.Value.HasValue == true)
            outdoorTemp = outdoorParam.Value.Value;

        var indoorTemp = 23.0;
        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        if (tempParam?.Value.HasValue == true)
            indoorTemp = tempParam.Value.Value;

        var roomZone = system.Zones
            .SelectMany(z => z.ZoneRooms)
            .FirstOrDefault(zr => zr.RoomId == request.RoomId);

        var roomMaxAirflow = config.DesignSupplyAirflow;
        var roomMinAirflow = config.MinimumSupplyAirflow;

        var demands = new List<VentilationDemand>();
        var primaryDemand = _demandFactory.CreateFromNeed(
            request.NeedId, request.RoomId, request.Severity,
            currentCo2, targetCo2, roomMaxAirflow, roomMinAirflow,
            system.Id.Value);
        demands.Add(primaryDemand);

        var strategyResult = _ventilationStrategy.Plan(
            demands, config, system, outdoorTemp, indoorTemp, null);

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var hvacPlan = strategyResult.Plan!;
        var totalAirflow = hvacPlan.TotalSupplyAirflow;

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            totalAirflow, "m3/h", "VentilationControlStrategy",
            hvacPlan.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: $"Ventilation: supply={hvacPlan.TotalSupplyAirflow:F0}, exhaust={hvacPlan.TotalExhaustAirflow:F0} m3/h",
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);

        var execResult = await _executor.ExecuteHvacPlanAsync(commandPlan, request.RoomId, system, hvacPlan, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "HVAC command plan execution failed");
    }

    private async Task<CapabilityPlanResultDto> PlanThermalAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.ThermalConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.ThermalConfigurationInvalid,
                "Thermal system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var outdoorTemp = 5.0;
        var outdoorParam = parameters.FirstOrDefault(p => p.Parameter == "outdoor_temperature");
        if (outdoorParam?.Value.HasValue == true) outdoorTemp = outdoorParam.Value.Value;

        var indoorTemp = 22.0;
        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        if (tempParam?.Value.HasValue == true) indoorTemp = tempParam.Value.Value;

        var isHeating = request.CapabilityCode == EngineeringCapabilityCodes.IncreaseTemperature;
        ThermalStrategyResult strategyResult;

        if (isHeating)
        {
            strategyResult = await _thermalStrategy.PlanHeatingAsync(system, outdoorTemp, indoorTemp,
                0, request.TargetValue ?? 22, indoorTemp, ct: ct);
        }
        else
        {
            strategyResult = _thermalStrategy.PlanCooling(system, outdoorTemp, indoorTemp,
                request.TargetValue ?? 22, indoorTemp);
        }

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var requestedValue = strategyResult.RequiredPowerKw;
        var valueUnit = "W";
        var effectDesc = isHeating
            ? $"Heating: source={strategyResult.SelectedHeatSource?.SourceType}, supply={strategyResult.TargetSupplyTemperature:F0}°C, power={requestedValue:F0}W"
            : $"Cooling: supply={strategyResult.TargetSupplyTemperature:F0}°C";

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            requestedValue, valueUnit, "ThermalStrategyEngine",
            strategyResult.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: effectDesc,
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);

        var execResult = await _executor.ExecuteAsync(commandPlan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Thermal command plan execution failed");
    }

    private async Task<CapabilityPlanResultDto> PlanHumidificationAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.HumidificationConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.HumidificationConfigurationInvalid,
                "Humidification system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var currentRh = 40.0;
        var rhParam = parameters.FirstOrDefault(p => p.Parameter == "humidity");
        if (rhParam?.Value.HasValue == true) currentRh = rhParam.Value.Value;

        var roomTemp = 22.0;
        var tempParam = parameters.FirstOrDefault(p => p.Parameter == "temperature");
        if (tempParam?.Value.HasValue == true) roomTemp = tempParam.Value.Value;

        var outdoorTemp = 5.0;
        var outdoorParam = parameters.FirstOrDefault(p => p.Parameter == "outdoor_temperature");
        if (outdoorParam?.Value.HasValue == true) outdoorTemp = outdoorParam.Value.Value;

        var targetRh = config.TargetRhPercent;
        var airflow = 300.0;

        var strategyResult = _humidificationStrategy.PlanHumidification(
            config, currentRh, targetRh, roomTemp, 14, airflow,
            outdoorTemp, null, HumidifierType.SteamElectrode);

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            strategyResult.RequiredCapacityKgH, "kg/h", "HumidificationStrategyEngine",
            strategyResult.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: $"Humidification: {strategyResult.RequiredCapacityKgH:F1} kg/h, steam={strategyResult.SteamOutputPct:F0}%",
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);

        var execResult = await _executor.ExecuteAsync(commandPlan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Humidification command plan execution failed");
    }

    private async Task<CapabilityPlanResultDto> PlanLightingAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CancellationToken ct)
    {
        var config = system.LightingConfiguration;
        if (config is null)
            return new CapabilityPlanResultDto(null, false,
                EngineeringErrors.LightingConfigurationInvalid,
                "Lighting system is not configured");

        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var currentLux = 300.0;
        var luxParam = parameters.FirstOrDefault(p => p.Parameter == "illuminance");
        if (luxParam?.Value.HasValue == true) currentLux = luxParam.Value.Value;

        var targetLux = request.CapabilityCode == EngineeringCapabilityCodes.IncreaseIlluminance
            ? config.DefaultBrightnessLux : config.MinimumBrightnessLux;

        var outdoorBrightness = 10000.0;
        var outdoorTemp = 25.0;

        var result = _lightingStrategy.PlanLighting(config, currentLux, targetLux,
            outdoorBrightness, outdoorTemp, 55.75, 37.62, DateTime.UtcNow, 3,
            3.5, 180, 20, null, null,
            request.CapabilityCode == EngineeringCapabilityCodes.IncreaseIlluminance);

        if (!result.IsSuccess)
            return new CapabilityPlanResultDto(null, false, result.FailureCode, result.FailureReason);

        var commandPlan = CommandPlan.Create(
            system.Id, request.CapabilityCode, request.CapabilityCode,
            result.TargetBrightnessLux, "lux", "LightingStrategyEngine",
            result.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            requestedEffect: $"Lighting: {result.TargetBrightnessLux} lux, {result.ColorTemperatureK}K, scene={result.AppliedScene}",
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(commandPlan, ct);
        var execResult = await _executor.ExecuteAsync(commandPlan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(commandPlan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Lighting command plan execution failed");
    }

    private async Task<CapabilityPlanResultDto> PlanStandardAsync(
        EngineeringCapabilityRequestDto request,
        EngineeringSystem system,
        CapabilityRoutingResult routing,
        CancellationToken ct)
    {
        var parameters = await _envReader.GetParametersAsync(request.RoomId, ct);
        var co2Param = parameters.FirstOrDefault(p => p.Parameter == "co2");
        var currentValue = co2Param?.Value ?? request.CurrentValue ?? 400;
        var deviation = request.Severity switch
        {
            "Critical" => 1000, "High" => 500, "Medium" => 200, _ => 100
        };

        var strategyResult = await _strategyEngine.PlanAsync(
            system, routing.CapabilityCode!, currentValue, deviation,
            request.TargetValue ?? 1000, ct);

        if (!strategyResult.IsSuccess)
            return new CapabilityPlanResultDto(null, false,
                strategyResult.FailureCode, strategyResult.FailureReason);

        var plan = CommandPlan.Create(
            system.Id, strategyResult.Plan!.NeedType, strategyResult.Plan.CapabilityCode,
            strategyResult.Plan.RequestedValue, strategyResult.Plan.ValueUnit,
            strategyResult.Plan.StrategyName, strategyResult.Plan.Steps.ToList(),
            needId: request.NeedId, buildingId: request.BuildingId,
            roomId: request.RoomId,
            correlationId: request.CorrelationId, causationId: request.CausationId,
            idempotencyKey: request.IdempotencyKey, expiresAt: request.ExpiresAt);

        await _planRepo.AddAsync(plan, ct);

        var execResult = await _executor.ExecuteAsync(plan, request.RoomId, system, ct);

        return new CapabilityPlanResultDto(
            new CommandPlanIdDto(plan.Id),
            execResult.Success,
            execResult.FailureCode,
            execResult.Success ? null : "Command plan execution failed");
    }

    public async Task<CommandPlanStatusDto?> GetCommandPlanStatusAsync(
        CommandPlanIdDto commandPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepo.GetByIdAsync(commandPlanId.Value, cancellationToken);
        if (plan is null) return null;

        return new CommandPlanStatusDto(
            commandPlanId, plan.Status.ToString(),
            plan.FailureCode, plan.CreatedAt, plan.CompletedAt);
    }

    public async Task<CancelCommandPlanResultDto> CancelCommandPlanAsync(
        CommandPlanIdDto commandPlanId, CancellationToken cancellationToken = default)
    {
        var plan = await _planRepo.GetByIdAsync(commandPlanId.Value, cancellationToken);
        if (plan is null)
            return new CancelCommandPlanResultDto(false, EngineeringErrors.CommandPlanNotFound);

        if (plan.Status is CommandPlanStatus.Succeeded or CommandPlanStatus.Failed
            or CommandPlanStatus.Cancelled or CommandPlanStatus.Expired)
            return new CancelCommandPlanResultDto(false, EngineeringErrors.CommandPlanAlreadyTerminal);

        plan.Cancel();
        await _planRepo.UpdateAsync(plan, cancellationToken);
        await _resourceManager.ReleaseAsync(plan, cancellationToken);
        return new CancelCommandPlanResultDto(true, null);
    }
}