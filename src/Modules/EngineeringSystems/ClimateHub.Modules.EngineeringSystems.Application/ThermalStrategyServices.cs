using ClimateHub.Modules.EngineeringSystems.Application.Thermal;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class ThermalStrategyResult
{
    public HeatSource? SelectedHeatSource { get; init; }
    public HydraulicCircuit? SelectedCircuit { get; init; }
    public double TargetSupplyTemperature { get; init; }
    public double RequiredPowerKw { get; init; }
    public bool HeatPumpDefrostSafe { get; init; }
    public HeatSourceSchedule? SourceSchedule { get; init; }
    public BufferPlan? BufferPlan { get; init; }
    public DHWPlan? DhwPlan { get; init; }
    public HydraulicAllocationPlan? HydraulicPlan { get; init; }
    public List<CommandPlanStep> Steps { get; init; } = new();
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public bool IsSuccess => FailureCode is null;
}

public class ThermalStrategyEngine
{
    private readonly WeatherCompensationPlanner _weatherPlanner;
    private readonly HeatLossEstimator _heatLossEstimator;
    private readonly HeatSourceScheduler _sourceScheduler;
    private readonly HydraulicDemandAllocator _hydraulicAllocator;
    private readonly BufferTankPlanner _bufferPlanner;
    private readonly DHWPlanner _dhwPlanner;
    private readonly ThermalDemandAggregator _demandAggregator;
    private readonly IHeatSourceRepository _heatSourceRepo;
    private readonly IHydraulicCircuitRepository _circuitRepo;
    private readonly IBufferTankRepository _bufferRepo;
    private readonly IDomesticHotWaterSystemRepository _dhwRepo;
    private readonly IWeatherCompensationCurveRepository _weatherCurveRepo;

    public ThermalStrategyEngine(
        WeatherCompensationPlanner weatherPlanner,
        HeatLossEstimator heatLossEstimator,
        HeatSourceScheduler sourceScheduler,
        HydraulicDemandAllocator hydraulicAllocator,
        BufferTankPlanner bufferPlanner,
        DHWPlanner dhwPlanner,
        ThermalDemandAggregator demandAggregator,
        IHeatSourceRepository heatSourceRepo,
        IHydraulicCircuitRepository circuitRepo,
        IBufferTankRepository bufferRepo,
        IDomesticHotWaterSystemRepository dhwRepo,
        IWeatherCompensationCurveRepository weatherCurveRepo)
    {
        _weatherPlanner = weatherPlanner; _heatLossEstimator = heatLossEstimator;
        _sourceScheduler = sourceScheduler; _hydraulicAllocator = hydraulicAllocator;
        _bufferPlanner = bufferPlanner; _dhwPlanner = dhwPlanner;
        _demandAggregator = demandAggregator;
        _heatSourceRepo = heatSourceRepo; _circuitRepo = circuitRepo;
        _bufferRepo = bufferRepo; _dhwRepo = dhwRepo; _weatherCurveRepo = weatherCurveRepo;
    }

    public async Task<ThermalStrategyResult> PlanHeatingAsync(
        EngineeringSystem system, double outdoorTemp, double indoorTemp,
        double windSpeed, double requiredTemp, double currentTemp,
        ThermalZone? zone = null, List<ThermalDemand>? demands = null,
        CancellationToken ct = default)
    {
        var config = system.ThermalConfiguration;
        if (config is null)
            return Fail(EngineeringErrors.ThermalConfigurationInvalid, "Thermal system not configured");

        ThermalZoneDemandPlan? zonePlan = null;
        if (zone is not null && demands is not null)
            zonePlan = _demandAggregator.Aggregate(zone, demands);

        var requiredPowerKw = zonePlan?.TotalRequiredHeatingPowerKw
            ?? _heatLossEstimator.Estimate(config, outdoorTemp, indoorTemp, windSpeed).RequiredPower / 1000.0;

        var weatherResult = _weatherPlanner.Calculate(config, outdoorTemp, indoorTemp);
        var targetSupply = weatherResult.TargetSupplyTemperature;

        targetSupply = Math.Min(targetSupply, config.MaximumSupplyTemperature);
        if (config.FreezeProtectionEnabled && outdoorTemp <= config.FreezeProtectionTemperature
            && targetSupply < config.FreezeProtectionSupplyTemperature)
            targetSupply = Math.Max(targetSupply, config.FreezeProtectionSupplyTemperature);

        // Load aggregates from repositories — no more config → aggregate conversion
        var heatSources = await _heatSourceRepo.GetAvailableAsync(system.Id, ct);
        var dhw = await _dhwRepo.GetByEngineeringSystemAsync(system.Id, ct);
        var buffer = await _bufferRepo.GetByEngineeringSystemAsync(system.Id, ct);

        var dhwPlan = _dhwPlanner.Plan(dhw, outdoorTemp);
        if (dhwPlan.DHWPriorityActive) requiredPowerKw += dhwPlan.RequiredHeatingPowerKw;

        var sourceSchedule = _sourceScheduler.Schedule(heatSources.ToList(), requiredPowerKw, targetSupply);
        if (sourceSchedule.FailureCode is not null)
            return Fail(sourceSchedule.FailureCode, "No available heat sources");

        var bufferPlan = _bufferPlanner.Plan(buffer, requiredPowerKw,
            sourceSchedule.SelectedSources.Any(s => s.IsMinimumRuntimeActive()),
            sourceSchedule.TotalScheduledPowerKw - requiredPowerKw);

        var circuits = await _circuitRepo.GetByEngineeringSystemAsync(system.Id, ct);
        var zoneDemands = new List<(Guid ZoneId, double RequiredPowerKw, int Priority)>();
        if (zone is not null)
            zoneDemands.Add((zone.Id.Value, requiredPowerKw * 1000, zone.Priority));
        var hydPlan = _hydraulicAllocator.Allocate(circuits.ToList(), zoneDemands, requiredPowerKw * 1000);
        var selectedCircuit = circuits.FirstOrDefault();

        var steps = BuildStartupSequence(sourceSchedule, targetSupply, selectedCircuit, bufferPlan);

        return new ThermalStrategyResult
        {
            SelectedHeatSource = sourceSchedule.SelectedSources.FirstOrDefault(),
            SelectedCircuit = selectedCircuit,
            TargetSupplyTemperature = targetSupply,
            RequiredPowerKw = requiredPowerKw,
            SourceSchedule = sourceSchedule,
            BufferPlan = bufferPlan,
            DhwPlan = dhwPlan,
            HydraulicPlan = hydPlan,
            Steps = steps
        };
    }

    private static List<CommandPlanStep> BuildStartupSequence(HeatSourceSchedule schedule,
        double targetSupply, HydraulicCircuit? circuit, BufferPlan bufferPlan)
    {
        var steps = new List<CommandPlanStep>();
        var seq = 0;

        if (circuit is not null)
        {
            steps.Add(new CommandPlanStep("control.pump-speed", "set",
                GetPumpSpeed(circuit.CircuitType), "percent",
                deviceRole: DeviceRole.CirculationPump, sequence: seq++, required: true));
        }

        steps.Add(new CommandPlanStep("control.valve-position", "set",
            MapTempToValvePct(targetSupply, 20, 90), "percent",
            deviceRole: DeviceRole.MixingValve, sequence: seq++, required: true));

        foreach (var source in schedule.StartOrder)
        {
            steps.Add(new CommandPlanStep("control.heat-source-enable", "set", 100, "percent",
                deviceRole: DeviceRole.HeatSourceBoiler, sequence: seq++, required: true));
        }

        steps.Add(new CommandPlanStep("control.valve-position", "set",
            MapTempToValvePct(targetSupply, 20, 90), "percent",
            deviceRole: DeviceRole.MixingValve, sequence: seq++, required: true));

        steps.Add(new CommandPlanStep("control.heating-output", "set",
            schedule.TotalScheduledPowerKw > 0 ? Math.Clamp(schedule.TotalScheduledPowerKw / 150.0 * 100, 10, 100) : 0,
            "percent", deviceRole: DeviceRole.RadiatorActuator, sequence: seq++,
            executionMode: ExecutionMode.Parallel, required: true));

        return steps;
    }

    public ThermalStrategyResult PlanCooling(
        EngineeringSystem system, double outdoorTemp, double indoorTemp,
        double requiredTemp, double currentTemp)
    {
        var config = system.ThermalConfiguration;
        if (config is null)
            return Fail(EngineeringErrors.ThermalConfigurationInvalid, "Thermal system not configured");

        var steps = new List<CommandPlanStep>
        {
            new("control.fan-coil-enable", "set", 100, "percent", deviceRole: DeviceRole.FanCoilUnit, sequence: 0, required: true),
            new("control.cooling-valve", "set", Math.Clamp((indoorTemp - requiredTemp) * 5, 10, 100), "percent", deviceRole: DeviceRole.MixingValve, sequence: 1, required: true),
            new("control.pump-speed", "set", 80, "percent", deviceRole: DeviceRole.CirculationPump, sequence: 2, required: true)
        };

        return new ThermalStrategyResult { TargetSupplyTemperature = 7, RequiredPowerKw = 3, Steps = steps };
    }

    private static ThermalStrategyResult Fail(string code, string reason) =>
        new() { FailureCode = code, FailureReason = reason };

    private static double GetPumpSpeed(HydraulicCircuitType type) => type switch
    {
        HydraulicCircuitType.FloorHeating => 60,
        HydraulicCircuitType.Radiators => 80,
        HydraulicCircuitType.FanCoil => 90,
        HydraulicCircuitType.DHW => 100,
        _ => 70
    };

    private static double MapTempToValvePct(double temp, double minTemp, double maxTemp)
    {
        var range = maxTemp - minTemp;
        return range > 0 ? Math.Clamp((temp - minTemp) / range * 100, 0, 100) : 50;
    }
}
