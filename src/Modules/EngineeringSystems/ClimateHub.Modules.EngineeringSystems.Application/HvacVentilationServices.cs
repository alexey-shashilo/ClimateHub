using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class VentilationDemandFactory
{
    public VentilationDemand CreateFromNeed(string needId, RoomId roomId, string severity,
        double currentCo2, double targetCo2, double roomMaxAirflow,
        double roomMinAirflow, Guid engineeringSystemId)
    {
        var preferred = VentilationDemand.CalculatePreferredAirflow(severity, roomMaxAirflow);
        return new VentilationDemand(engineeringSystemId, needId, roomId, severity,
            currentCo2, targetCo2, roomMinAirflow, preferred, roomMaxAirflow);
    }
}

public class VentilationDemandAggregator
{
    public VentilationDemandPlan Aggregate(
        List<VentilationDemand> activeDemands,
        VentilationSystemConfiguration config,
        EngineeringSystem system)
    {
        var supplyResource = system.GetResource("supply_airflow_capacity")
                             ?? system.GetResource("airflow_capacity");
        var exhaustResource = system.GetResource("exhaust_airflow_capacity")
                              ?? system.GetResource("airflow_capacity");

        var availableSupply = supplyResource?.Available ?? config.DesignSupplyAirflow;
        var availableExhaust = exhaustResource?.Available ?? config.DesignExhaustAirflow;

        var plan = new VentilationDemandPlan(system.Id.Value, activeDemands, availableSupply, availableExhaust);

        if (activeDemands.Count == 0)
        {
            plan.SetResult(AllocationResult.FullyAllocated);
            return plan;
        }

        var totalRequested = activeDemands.Sum(d => d.PreferredAirflow);
        var totalMinRequired = activeDemands.Sum(d => d.MinimumAirflow);

        if (totalMinRequired > Math.Min(availableSupply, availableExhaust))
        {
            plan.SetResult(AllocationResult.InsufficientCapacity);
            return plan;
        }

        var severityOrder = new Dictionary<string, int> { ["Critical"] = 0, ["High"] = 1, ["Medium"] = 2, ["Low"] = 3 };
        var sorted = activeDemands
            .OrderBy(d => severityOrder.GetValueOrDefault(d.Severity, 99))
            .ThenBy(d => d.Priority)
            .ToList();

        double remainingSupply = availableSupply;
        double remainingExhaust = availableExhaust;
        double unserved = 0;

        foreach (var demand in sorted)
        {
            var request = Math.Min(demand.PreferredAirflow, demand.MaximumAirflow);
            var supplyAlloc = Math.Min(request, remainingSupply);
            var exhaustAlloc = Math.Min(request, remainingExhaust);

            if (supplyAlloc < demand.MinimumAirflow)
            {
                unserved += demand.PreferredAirflow;
                plan.AddAllocation(new RoomAirflowAllocation(
                    Guid.Empty, demand.RoomId, demand.PreferredAirflow, 0, 0,
                    demand.Priority, "Unserved"));
                continue;
            }

            var quality = supplyAlloc >= demand.PreferredAirflow ? "Full" : "Partial";
            plan.AddAllocation(new RoomAirflowAllocation(
                Guid.Empty, demand.RoomId, demand.PreferredAirflow,
                supplyAlloc, exhaustAlloc, demand.Priority, quality));

            remainingSupply -= supplyAlloc;
            remainingExhaust -= exhaustAlloc;
        }

        plan.SetResult(remainingSupply > 0 ? AllocationResult.FullyAllocated : AllocationResult.PartiallyAllocated);
        plan.UnservedDemand = unserved;
        return plan;
    }
}

public class AirflowBalancePlanner
{
    public BalancedAirflowPlan Plan(double totalSupply, double totalExhaust, double maxImbalancePct)
    {
        if (totalSupply <= 0 && totalExhaust <= 0)
            return new BalancedAirflowPlan(0, 0, maxImbalancePct);

        var plan = new BalancedAirflowPlan(totalSupply, totalExhaust, maxImbalancePct);

        if (plan.Status == BalanceStatus.Imbalanced)
        {
            var minFlow = Math.Min(totalSupply, totalExhaust);
            var adjusted = minFlow * (1 - maxImbalancePct / 100);
            return new BalancedAirflowPlan(adjusted, adjusted, maxImbalancePct);
        }

        return plan;
    }
}

public class FanOutputMapper
{
    public FanMappingResult MapToSpeed(double requestedAirflow, double designAirflow,
        double minSpeedPct = 0, double maxSpeedPct = 100)
    {
        if (designAirflow <= 0)
            return new FanMappingResult(0, 0, "Invalid");

        var speedPct = requestedAirflow / designAirflow * 100;
        speedPct = Math.Clamp(speedPct, minSpeedPct, maxSpeedPct);
        var estimatedAirflow = speedPct / 100 * designAirflow;
        var quality = requestedAirflow > 0 ? "Estimated" : "Off";

        return new FanMappingResult(Math.Round(speedPct, 0), Math.Round(estimatedAirflow, 1), quality);
    }
}

public record FanMappingResult(double SpeedPct, double EstimatedAirflow, string Quality);

public class DamperPositionMapper
{
    public DamperMappingResult MapToPosition(double allocatedAirflow, double maxAirflow,
        double minOpenPct = 0, double maxOpenPct = 100)
    {
        if (maxAirflow <= 0 || allocatedAirflow <= 0)
            return new DamperMappingResult(0, 0, "Closed");

        var positionPct = allocatedAirflow / maxAirflow * 100;
        positionPct = Math.Clamp(positionPct, minOpenPct, maxOpenPct);
        var estimatedAirflow = positionPct / 100 * maxAirflow;

        return new DamperMappingResult(Math.Round(positionPct, 0), Math.Round(estimatedAirflow, 1), "LinearEstimate");
    }
}

public record DamperMappingResult(double PositionPct, double EstimatedAirflow, string Quality);

public class HeatRecoveryPlanner
{
    public HeatRecoveryPlan Plan(double outdoorTemp, double indoorTemp,
        double? supplyAirTemp, double targetSupplyTemp,
        VentilationSystemConfiguration config)
    {
        if (!config.HasHeatRecovery)
            return new HeatRecoveryPlan(false, 100, 0, null, "No heat recovery available", "Unavailable");

        if (double.IsNaN(outdoorTemp) || double.IsNaN(indoorTemp))
            return new HeatRecoveryPlan(false, 100, 0, null, "Temperature sensors unavailable", "Unknown");

        if (outdoorTemp < indoorTemp)
        {
            var efficiency = config.DefaultHeatRecoveryEfficiency;
            var expectedTemp = outdoorTemp + (indoorTemp - outdoorTemp) * efficiency;
            var bypassPos = 0;

            if (outdoorTemp > 16 && indoorTemp > 24)
            {
                bypassPos = 100;
                expectedTemp = outdoorTemp;
                return new HeatRecoveryPlan(true, bypassPos, efficiency, expectedTemp, "Free cooling bypass", "FreeCooling");
            }

            return new HeatRecoveryPlan(true, bypassPos, efficiency, expectedTemp, "Heat recovery active", "Active");
        }

        return new HeatRecoveryPlan(true, 100, 0, outdoorTemp, "Bypass: outdoor air suitable", "Bypass");
    }
}

public record HeatRecoveryPlan(bool Enabled, double BypassPosition, double ExpectedEfficiency,
    double? ExpectedSupplyTemp, string Reason, string Quality);

public class SupplyAirTemperaturePlanner
{
    public SupplyAirConditioningPlan Plan(double outdoorTemp, HeatRecoveryPlan recoveryPlan,
        double? measuredSupplyTemp, VentilationSystemConfiguration config)
    {
        var expectedTemp = recoveryPlan.ExpectedSupplyTemp ?? outdoorTemp;

        if (expectedTemp >= config.MinimumSupplyAirTemperature && expectedTemp <= config.MaximumSupplyAirTemperature)
            return new SupplyAirConditioningPlan(false, 0, expectedTemp, "Safe", null);

        if (expectedTemp < config.MinimumSupplyAirTemperature)
        {
            if (!config.HasSupplyHeater)
                return new SupplyAirConditioningPlan(false, 0, expectedTemp, "Unsafe",
                    EngineeringErrors.SupplyAirTemperatureUnsafe);

            var needed = config.MinimumSupplyAirTemperature - expectedTemp;
            var maxRise = 20.0;
            var outputPct = Math.Clamp(needed / maxRise * 100, 0, 100);
            var expectedWithHeater = expectedTemp + (maxRise * outputPct / 100);

            return new SupplyAirConditioningPlan(true, Math.Round(outputPct, 0), Math.Round(expectedWithHeater, 1),
                "HeaterRequired", null);
        }

        return new SupplyAirConditioningPlan(false, 0, expectedTemp, "Warning", null);
    }
}

public record SupplyAirConditioningPlan(bool HeaterRequired, double HeaterOutputPct,
    double ExpectedSupplyTemperature, string SafetyStatus, string? FailureCode);

public class FrostProtectionPolicy
{
    public FrostProtectionStatus Evaluate(double outdoorTemp, double? supplyAirTemp,
        SupplyAirConditioningPlan tempPlan, HeatRecoveryPlan recoveryPlan,
        VentilationSystemConfiguration config)
    {
        if (!config.FrostProtectionEnabled)
            return new FrostProtectionStatus("Normal", "Frost protection disabled", null);

        if (double.IsNaN(outdoorTemp) && !config.HasOutdoorTemperatureSensor)
            return new FrostProtectionStatus("Unknown", "Outdoor temperature unavailable", null);

        if (outdoorTemp <= config.FrostProtectionTemperature)
        {
            if (tempPlan.SafetyStatus == "Unsafe")
                return new FrostProtectionStatus("Critical",
                    $"Outdoor {outdoorTemp}°C below frost limit {config.FrostProtectionTemperature}°C and supply air unsafe",
                    EngineeringErrors.FrostProtectionActive);

            if (config.HasSupplyHeater && tempPlan.HeaterRequired)
                return new FrostProtectionStatus("ActiveProtection",
                    $"Outdoor {outdoorTemp}°C, heater active", null);

            return new FrostProtectionStatus("Risk",
                $"Outdoor {outdoorTemp}°C approaching frost limit", null);
        }

        return new FrostProtectionStatus("Normal", $"Outdoor {outdoorTemp}°C above frost limit", null);
    }
}

public record FrostProtectionStatus(string Status, string Description, string? FailureCode);

public class HvacSafeStopPlanner
{
    public List<CommandPlanStep> PlanSafeStop(CommandPlan failedPlan, EngineeringSystem system)
    {
        var steps = new List<CommandPlanStep>();
        var existingSteps = failedPlan.Steps.ToList();
        var seq = 100;

        if (existingSteps.Any(s => s.DeviceRole == DeviceRole.SupplyHeater))
        {
            steps.Add(new CommandPlanStep("control.heater-output", "set", 0, "percent",
                deviceRole: DeviceRole.SupplyHeater, sequence: seq++, required: false));
        }

        if (existingSteps.Any(s => s.DeviceRole == DeviceRole.SupplyFan))
        {
            steps.Add(new CommandPlanStep("control.fan-speed", "set", 0, "percent",
                deviceRole: DeviceRole.SupplyFan, sequence: seq++, required: false));
        }

        if (existingSteps.Any(s => s.DeviceRole == DeviceRole.ExhaustFan))
        {
            steps.Add(new CommandPlanStep("control.fan-speed", "set", 0, "percent",
                deviceRole: DeviceRole.ExhaustFan, sequence: seq++, required: false));
        }

        if (existingSteps.Any(s => s.DeviceRole == DeviceRole.SupplyDamper))
        {
            steps.Add(new CommandPlanStep("control.damper-position", "set", 15, "percent",
                deviceRole: DeviceRole.SupplyDamper, sequence: seq++, required: false));
        }

        return steps;
    }
}

public class VentilationControlStrategy
{
    private readonly VentilationDemandAggregator _demandAggregator;
    private readonly AirflowBalancePlanner _balancePlanner;
    private readonly FanOutputMapper _fanMapper;
    private readonly DamperPositionMapper _damperMapper;
    private readonly HeatRecoveryPlanner _recoveryPlanner;
    private readonly SupplyAirTemperaturePlanner _tempPlanner;
    private readonly FrostProtectionPolicy _frostPolicy;
    private readonly VentilationDemandFactory _demandFactory;

    public VentilationControlStrategy(
        VentilationDemandAggregator demandAggregator,
        AirflowBalancePlanner balancePlanner,
        FanOutputMapper fanMapper,
        DamperPositionMapper damperMapper,
        HeatRecoveryPlanner recoveryPlanner,
        SupplyAirTemperaturePlanner tempPlanner,
        FrostProtectionPolicy frostPolicy,
        VentilationDemandFactory demandFactory)
    {
        _demandAggregator = demandAggregator; _balancePlanner = balancePlanner;
        _fanMapper = fanMapper; _damperMapper = damperMapper;
        _recoveryPlanner = recoveryPlanner; _tempPlanner = tempPlanner;
        _frostPolicy = frostPolicy; _demandFactory = demandFactory;
    }

    public VentilationStrategyResult Plan(List<VentilationDemand> demands,
        VentilationSystemConfiguration config, EngineeringSystem system,
        double outdoorTemp, double indoorTemp, double? supplyAirTemp)
    {
        var demandPlan = _demandAggregator.Aggregate(demands, config, system);
        if (demandPlan.Result == AllocationResult.InsufficientCapacity)
            return new VentilationStrategyResult(null, EngineeringErrors.AirflowCapacityInsufficient,
                "Insufficient ventilation capacity for minimum room demands");

        var totalSupply = demandPlan.TotalAllocatedSupplyAirflow;
        var totalExhaust = demandPlan.TotalAllocatedExhaustAirflow;

        var balance = _balancePlanner.Plan(totalSupply, totalExhaust, config.MaximumImbalancePct);
        demandPlan.SetBalance(balance);

        if (balance.Status == BalanceStatus.Imbalanced || balance.Status == BalanceStatus.Unachievable)
            return new VentilationStrategyResult(null, EngineeringErrors.AirflowBalanceUnachievable,
                $"Cannot achieve airflow balance: supply={totalSupply:F1}, exhaust={totalExhaust:F1}");

        var balancedSupply = balance.TotalSupplyAirflow;
        var balancedExhaust = balance.TotalExhaustAirflow;

        var supplyFan = _fanMapper.MapToSpeed(balancedSupply, config.DesignSupplyAirflow);
        var exhaustFan = _fanMapper.MapToSpeed(balancedExhaust, config.DesignExhaustAirflow);

        var heatRecovery = _recoveryPlanner.Plan(outdoorTemp, indoorTemp,
            supplyAirTemp, config.MinimumSupplyAirTemperature, config);

        var tempPlan = _tempPlanner.Plan(outdoorTemp, heatRecovery,
            supplyAirTemp, config);

        var frostStatus = _frostPolicy.Evaluate(outdoorTemp, supplyAirTemp,
            tempPlan, heatRecovery, config);

        if (frostStatus.Status == "Critical")
            return new VentilationStrategyResult(null, frostStatus.FailureCode ?? EngineeringErrors.FrostProtectionActive,
                frostStatus.Description);

        var steps = new List<CommandPlanStep>();
        var seq = 0;

        var roomDamperCommands = BuildDamperSteps(demandPlan, config, seq);
        steps.AddRange(roomDamperCommands);
        seq = roomDamperCommands.Count > 0 ? roomDamperCommands.Max(s => s.Sequence) + 1 : 0;

        if (config.HasHeatRecovery && heatRecovery.Enabled)
        {
            steps.Add(new CommandPlanStep("control.heat-recovery-bypass", "set",
                heatRecovery.BypassPosition, "percent",
                deviceRole: DeviceRole.Recuperator, sequence: seq++, required: false));
        }

        if (tempPlan.HeaterRequired && config.HasSupplyHeater)
        {
            steps.Add(new CommandPlanStep("control.heater-output", "set",
                tempPlan.HeaterOutputPct, "percent",
                deviceRole: DeviceRole.SupplyHeater, sequence: seq++, required: true));
        }

        steps.Add(new CommandPlanStep("control.fan-speed", "set",
            exhaustFan.SpeedPct, "percent",
            deviceRole: DeviceRole.ExhaustFan, sequence: seq++,
            executionMode: ExecutionMode.Parallel, required: true));

        steps.Add(new CommandPlanStep("control.fan-speed", "set",
            supplyFan.SpeedPct, "percent",
            deviceRole: DeviceRole.SupplyFan, sequence: seq++,
            executionMode: ExecutionMode.Parallel, required: true));

        return new VentilationStrategyResult(
            new VentilationCommandPlanDefinition(
                balancedSupply, balancedExhaust,
                supplyFan.SpeedPct, exhaustFan.SpeedPct,
                demandPlan, balance, heatRecovery, tempPlan, frostStatus,
                steps, config.MinimumSupplyAirTemperature),
            null, null);
    }

    private List<CommandPlanStep> BuildDamperSteps(VentilationDemandPlan demandPlan,
        VentilationSystemConfiguration config, int startSeq)
    {
        var steps = new List<CommandPlanStep>();
        var seq = startSeq;

        foreach (var alloc in demandPlan.Allocations)
        {
            if (alloc.AllocatedSupplyAirflow <= 0) continue;

            var damperPos = _damperMapper.MapToPosition(alloc.AllocatedSupplyAirflow,
                config.DesignSupplyAirflow);
            steps.Add(new CommandPlanStep("control.damper-position", "set",
                damperPos.PositionPct, "percent",
                deviceRole: DeviceRole.SupplyDamper, sequence: seq++, required: true));
        }

        return steps;
    }
}

public record VentilationStrategyResult(
    VentilationCommandPlanDefinition? Plan,
    string? FailureCode,
    string? FailureReason)
{
    public bool IsSuccess => Plan is not null;
}

public class VentilationCommandPlanDefinition
{
    public double TotalSupplyAirflow { get; }
    public double TotalExhaustAirflow { get; }
    public double SupplyFanSpeedPct { get; }
    public double ExhaustFanSpeedPct { get; }
    public VentilationDemandPlan DemandPlan { get; }
    public BalancedAirflowPlan BalancePlan { get; }
    public HeatRecoveryPlan HeatRecoveryPlan { get; }
    public SupplyAirConditioningPlan TempPlan { get; }
    public FrostProtectionStatus FrostStatus { get; }
    public List<CommandPlanStep> Steps { get; }
    public double MinimumSupplyAirTemperature { get; }

    public VentilationCommandPlanDefinition(
        double totalSupply, double totalExhaust,
        double supplyFanSpeed, double exhaustFanSpeed,
        VentilationDemandPlan demandPlan,
        BalancedAirflowPlan balancePlan,
        HeatRecoveryPlan heatRecoveryPlan,
        SupplyAirConditioningPlan tempPlan,
        FrostProtectionStatus frostStatus,
        List<CommandPlanStep> steps,
        double minSupplyTemp)
    {
        TotalSupplyAirflow = totalSupply; TotalExhaustAirflow = totalExhaust;
        SupplyFanSpeedPct = supplyFanSpeed; ExhaustFanSpeedPct = exhaustFanSpeed;
        DemandPlan = demandPlan; BalancePlan = balancePlan;
        HeatRecoveryPlan = heatRecoveryPlan; TempPlan = tempPlan;
        FrostStatus = frostStatus; Steps = steps;
        MinimumSupplyAirTemperature = minSupplyTemp;
    }
}