using ClimateHub.Modules.EngineeringSystems.Application;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.SharedKernel.Primitives;
using static ClimateHub.Modules.EngineeringSystems.Domain.DeviceRole;

namespace ClimateHub.Modules.EngineeringSystems.UnitTests;

public class VentilationDemandAggregatorTests
{
    private readonly RoomId _roomA = RoomId.From(Guid.NewGuid());
    private readonly RoomId _roomB = RoomId.From(Guid.NewGuid());
    private readonly RoomId _roomC = RoomId.From(Guid.NewGuid());

    [Fact]
    public void Aggregate_NoDemands_ReturnsFullyAllocated()
    {
        var agg = new VentilationDemandAggregator();
        var config = new VentilationSystemConfiguration(Guid.NewGuid());
        var system = CreateSystemWithAirflow(500);

        var plan = agg.Aggregate(new List<VentilationDemand>(), config, system);

        Assert.Equal(AllocationResult.FullyAllocated, plan.Result);
    }

    [Fact]
    public void Aggregate_MultiRoom_OrdersBySeverityThenPriority()
    {
        var agg = new VentilationDemandAggregator();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), designSupplyAirflow: 500, designExhaustAirflow: 500);
        var system = CreateSystemWithAirflow(500);

        var demands = new List<VentilationDemand>
        {
            CreateDemand(_roomA, "Low", 90),
            CreateDemand(_roomB, "Critical", 200),
            CreateDemand(_roomC, "Medium", 100)
        };

        var plan = agg.Aggregate(demands, config, system);

        Assert.Equal(3, plan.Allocations.Count);
        Assert.Equal(_roomB, plan.Allocations[0].RoomId);
        Assert.Equal(_roomC, plan.Allocations[1].RoomId);
        Assert.Equal(_roomA, plan.Allocations[2].RoomId);
    }

    [Fact]
    public void Aggregate_CapacityShortage_ReturnsInsufficientCapacity()
    {
        var agg = new VentilationDemandAggregator();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), designSupplyAirflow: 30, designExhaustAirflow: 30);
        var system = CreateSystemWithAirflow(30);

        var demands = new List<VentilationDemand>
        {
            CreateDemand(_roomA, "Medium", 50, minAirflow: 40)
        };

        var plan = agg.Aggregate(demands, config, system);

        Assert.Equal(AllocationResult.InsufficientCapacity, plan.Result);
    }

    [Fact]
    public void Aggregate_PartialAllocation_MarksPartialQuality()
    {
        var agg = new VentilationDemandAggregator();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), designSupplyAirflow: 200, designExhaustAirflow: 200);
        var system = CreateSystemWithAirflow(200);

        var demands = new List<VentilationDemand>
        {
            CreateDemand(_roomA, "Critical", 150, minAirflow: 50),
            CreateDemand(_roomB, "High", 80, minAirflow: 30)
        };

        var plan = agg.Aggregate(demands, config, system);

        Assert.Equal(AllocationResult.PartiallyAllocated, plan.Result);
    }

    [Fact]
    public void Aggregate_PriorityOrdering_LowerPrioritySkippedWhenCapacityExhausted()
    {
        var agg = new VentilationDemandAggregator();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), designSupplyAirflow: 100, designExhaustAirflow: 100);
        var system = CreateSystemWithAirflow(100);

        var demands = new List<VentilationDemand>
        {
            CreateDemand(_roomA, "Critical", 100, minAirflow: 50, priority: 1),
            CreateDemand(_roomB, "Critical", 80, minAirflow: 50, priority: 2)
        };

        var plan = agg.Aggregate(demands, config, system);

        Assert.Contains(plan.Allocations, a => a.AllocationQuality == "Unserved");
    }

    private static VentilationDemand CreateDemand(RoomId room, string severity, double preferredAirflow,
        double minAirflow = 10, int priority = 100)
    {
        return new VentilationDemand(Guid.NewGuid(), Guid.NewGuid().ToString(), room,
            severity, 800, 600, minAirflow, preferredAirflow, preferredAirflow * 1.2, priority);
    }

    private static EngineeringSystem CreateSystemWithAirflow(double airflow)
    {
        var sys = EngineeringSystem.Create(BuildingId.From(Guid.NewGuid()), "Test", SystemType.Ventilation);
        sys.AddResource(new EngineeringResource("airflow_capacity", airflow));
        return sys;
    }
}

public class AirflowBalancePlannerTests
{
    [Fact]
    public void Plan_EqualSupplyExhaust_ReturnsBalanced()
    {
        var planner = new AirflowBalancePlanner();
        var plan = planner.Plan(200, 200, 10);
        Assert.Equal(BalanceStatus.Balanced, plan.Status);
        Assert.Equal(200, plan.TotalSupplyAirflow);
    }

    [Fact]
    public void Plan_BothZero_ReturnsZeroPlan()
    {
        var planner = new AirflowBalancePlanner();
        var plan = planner.Plan(0, 0, 10);
        Assert.Equal(0, plan.TotalSupplyAirflow);
        Assert.Equal(0, plan.TotalExhaustAirflow);
    }

    [Fact]
    public void Plan_SlightImbalanceWithinTolerance_ReturnsBalanced()
    {
        var planner = new AirflowBalancePlanner();
        var plan = planner.Plan(200, 195, 5);
        Assert.Equal(BalanceStatus.Balanced, plan.Status);
    }

    [Fact]
    public void Plan_ImbalanceExceedsTolerance_ReturnsAdjustedBalancedPlan()
    {
        var planner = new AirflowBalancePlanner();
        var plan = planner.Plan(300, 200, 10);
        Assert.Equal(BalanceStatus.Balanced, plan.Status);
        Assert.True(plan.TotalSupplyAirflow < 300);
    }

    [Fact]
    public void Plan_Imbalanced_AdjustsToMinFlow()
    {
        var planner = new AirflowBalancePlanner();
        var plan = planner.Plan(300, 100, 5);
        var minFlow = Math.Min(300, 100);
        var expected = minFlow * (1 - 5.0 / 100);
        Assert.Equal(expected, plan.TotalSupplyAirflow);
        Assert.Equal(expected, plan.TotalExhaustAirflow);
    }
}

public class FanOutputMapperTests
{
    [Fact]
    public void MapToSpeed_DesignAirflow_ReturnsFullSpeed()
    {
        var mapper = new FanOutputMapper();
        var result = mapper.MapToSpeed(300, 300);
        Assert.Equal(100, result.SpeedPct);
        Assert.Equal("Estimated", result.Quality);
    }

    [Fact]
    public void MapToSpeed_HalfAirflow_ReturnsFiftyPercent()
    {
        var mapper = new FanOutputMapper();
        var result = mapper.MapToSpeed(150, 300);
        Assert.Equal(50, result.SpeedPct);
    }

    [Fact]
    public void MapToSpeed_ZeroAirflow_ReturnsOff()
    {
        var mapper = new FanOutputMapper();
        var result = mapper.MapToSpeed(0, 300);
        Assert.Equal(0, result.SpeedPct);
        Assert.Equal("Off", result.Quality);
    }

    [Fact]
    public void MapToSpeed_InvalidDesignAirflow_ReturnsInvalid()
    {
        var mapper = new FanOutputMapper();
        var result = mapper.MapToSpeed(100, 0);
        Assert.Equal("Invalid", result.Quality);
    }

    [Fact]
    public void MapToSpeed_ClampsToMaxSpeed()
    {
        var mapper = new FanOutputMapper();
        var result = mapper.MapToSpeed(500, 300, maxSpeedPct: 90);
        Assert.Equal(90, result.SpeedPct);
    }

    [Fact]
    public void MapToSpeed_ClampsToMinSpeed()
    {
        var mapper = new FanOutputMapper();
        var result = mapper.MapToSpeed(10, 300, minSpeedPct: 20);
        Assert.Equal(20, result.SpeedPct);
    }

    [Fact]
    public void MapToSpeed_BoundaryFullAndZero()
    {
        var mapper = new FanOutputMapper();
        var full = mapper.MapToSpeed(300, 300);
        Assert.Equal(100, full.SpeedPct);
        var none = mapper.MapToSpeed(0, 300);
        Assert.Equal(0, none.SpeedPct);
    }
}

public class DamperPositionMapperTests
{
    [Fact]
    public void MapToPosition_ValidAirflow_ReturnsLinearPosition()
    {
        var mapper = new DamperPositionMapper();
        var result = mapper.MapToPosition(90, 300);
        Assert.Equal(30, result.PositionPct);
        Assert.Equal("LinearEstimate", result.Quality);
    }

    [Fact]
    public void MapToPosition_ZeroAllocation_ReturnsClosed()
    {
        var mapper = new DamperPositionMapper();
        var result = mapper.MapToPosition(0, 300);
        Assert.Equal(0, result.PositionPct);
        Assert.Equal("Closed", result.Quality);
    }

    [Fact]
    public void MapToPosition_ZeroMaxAirflow_ReturnsClosed()
    {
        var mapper = new DamperPositionMapper();
        var result = mapper.MapToPosition(90, 0);
        Assert.Equal(0, result.PositionPct);
        Assert.Equal("Closed", result.Quality);
    }

    [Fact]
    public void MapToPosition_FullAirflow_ReturnsMaxOpen()
    {
        var mapper = new DamperPositionMapper();
        var result = mapper.MapToPosition(300, 300);
        Assert.Equal(100, result.PositionPct);
    }

    [Fact]
    public void MapToPosition_ClampsWithinBounds()
    {
        var mapper = new DamperPositionMapper();
        var result = mapper.MapToPosition(500, 300, minOpenPct: 20, maxOpenPct: 80);
        Assert.Equal(80, result.PositionPct);
        result = mapper.MapToPosition(10, 300, minOpenPct: 20, maxOpenPct: 80);
        Assert.Equal(20, result.PositionPct);
    }
}

public class HeatRecoveryPlannerTests
{
    [Fact]
    public void Plan_NoHeatRecovery_ReturnsUnavailable()
    {
        var planner = new HeatRecoveryPlanner();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), hasHeatRecovery: false);

        var plan = planner.Plan(5, 22, 18, 16, config);

        Assert.False(plan.Enabled);
        Assert.Equal("Unavailable", plan.Quality);
    }

    [Fact]
    public void Plan_InvalidTemperatures_ReturnsUnknown()
    {
        var planner = new HeatRecoveryPlanner();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), hasHeatRecovery: true);

        var plan = planner.Plan(double.NaN, 22, 18, 16, config);

        Assert.Equal("Unknown", plan.Quality);
    }

    [Fact]
    public void Plan_ColdOutdoor_HeatRecoveryActive()
    {
        var planner = new HeatRecoveryPlanner();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), hasHeatRecovery: true, defaultHeatRecoveryEfficiency: 0.75);

        var plan = planner.Plan(-5, 22, 18, 16, config);

        Assert.True(plan.Enabled);
        Assert.Equal("Active", plan.Quality);
        Assert.True(plan.ExpectedSupplyTemp < 22);
    }

    [Fact]
    public void Plan_WarmOutdoor_FreeCoolingBypass()
    {
        var planner = new HeatRecoveryPlanner();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), hasHeatRecovery: true);

        var plan = planner.Plan(18, 26, 20, 16, config);

        Assert.True(plan.Enabled);
        Assert.Equal("FreeCooling", plan.Quality);
        Assert.Equal(100, plan.BypassPosition);
    }
}

public class FrostProtectionPolicyTests
{
    [Fact]
    public void Evaluate_Disabled_ReturnsNormal()
    {
        var policy = new FrostProtectionPolicy();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), frostProtectionEnabled: false, frostProtectionTemperature: -15);

        var status = policy.Evaluate(-20, 10, new SupplyAirConditioningPlan(false, 0, 16, "Safe", null),
            new HeatRecoveryPlan(false, 0, 0, null, "", "Unavailable"), config);

        Assert.Equal("Normal", status.Status);
    }

    [Fact]
    public void Evaluate_BelowFrostLimitWithUnsafeSupply_ReturnsCritical()
    {
        var policy = new FrostProtectionPolicy();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), frostProtectionEnabled: true, frostProtectionTemperature: -5);

        var status = policy.Evaluate(-10, null, new SupplyAirConditioningPlan(false, 0, 5, "Unsafe", EngineeringErrors.SupplyAirTemperatureUnsafe),
            new HeatRecoveryPlan(true, 0, 0.75, 5, "", "Active"), config);

        Assert.Equal("Critical", status.Status);
        Assert.Equal(EngineeringErrors.FrostProtectionActive, status.FailureCode);
    }

    [Fact]
    public void Evaluate_AboveFrostLimit_ReturnsNormal()
    {
        var policy = new FrostProtectionPolicy();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), frostProtectionEnabled: true, frostProtectionTemperature: -15);

        var status = policy.Evaluate(-2, 18, new SupplyAirConditioningPlan(false, 0, 18, "Safe", null),
            new HeatRecoveryPlan(true, 0, 0.75, 18, "", "Active"), config);

        Assert.Equal("Normal", status.Status);
    }

    [Fact]
    public void Evaluate_BelowLimitWithHeater_ReturnsActiveProtection()
    {
        var policy = new FrostProtectionPolicy();
        var config = new VentilationSystemConfiguration(Guid.NewGuid(), frostProtectionEnabled: true, frostProtectionTemperature: -5, hasSupplyHeater: true);

        var status = policy.Evaluate(-8, 15, new SupplyAirConditioningPlan(true, 30, 18, "HeaterRequired", null),
            new HeatRecoveryPlan(true, 0, 0.75, -5, "", "Active"), config);

        Assert.Equal("ActiveProtection", status.Status);
    }
}

public class HvacSafeStopPlannerTests
{
    [Fact]
    public void PlanSafeStop_WithHeater_AddsHeaterStep()
    {
        var planner = new HvacSafeStopPlanner();
        var system = CreateSystem();
        var plan = CreatePlanWithRole(SupplyHeater);

        var steps = planner.PlanSafeStop(plan, system);

        Assert.Contains(steps, s => s.DeviceRole == SupplyHeater && s.RequestedValue == 0);
    }

    [Fact]
    public void PlanSafeStop_WithSupplyFan_AddsFanStep()
    {
        var planner = new HvacSafeStopPlanner();
        var system = CreateSystem();
        var plan = CreatePlanWithRole(SupplyFan);

        var steps = planner.PlanSafeStop(plan, system);

        Assert.Contains(steps, s => s.DeviceRole == SupplyFan && s.RequestedValue == 0);
    }

    [Fact]
    public void PlanSafeStop_WithExhaustFan_AddsExhaustFanStep()
    {
        var planner = new HvacSafeStopPlanner();
        var system = CreateSystem();
        var plan = CreatePlanWithRole(ExhaustFan);

        var steps = planner.PlanSafeStop(plan, system);

        Assert.Contains(steps, s => s.DeviceRole == ExhaustFan && s.RequestedValue == 0);
    }

    [Fact]
    public void PlanSafeStop_WithDamper_SetsToFifteenPercent()
    {
        var planner = new HvacSafeStopPlanner();
        var system = CreateSystem();
        var plan = CreatePlanWithRole(SupplyDamper);

        var steps = planner.PlanSafeStop(plan, system);

        Assert.Contains(steps, s => s.DeviceRole == SupplyDamper && s.RequestedValue == 15);
    }

    [Fact]
    public void PlanSafeStop_AllDevices_AddsAllSteps()
    {
        var planner = new HvacSafeStopPlanner();
        var system = CreateSystem();
        var plan = CreatePlanAllRoles();

        var steps = planner.PlanSafeStop(plan, system);

        Assert.Contains(steps, s => s.DeviceRole == SupplyHeater);
        Assert.Contains(steps, s => s.DeviceRole == SupplyFan);
        Assert.Contains(steps, s => s.DeviceRole == ExhaustFan);
        Assert.Contains(steps, s => s.DeviceRole == SupplyDamper);
    }

    [Fact]
    public void PlanSafeStop_NoDeviceMatch_ReturnsEmpty()
    {
        var planner = new HvacSafeStopPlanner();
        var system = CreateSystem();
        var plan = CreatePlanWithRole(Recuperator);

        var steps = planner.PlanSafeStop(plan, system);

        Assert.Empty(steps);
    }

    private static CommandPlan CreatePlanWithRole(string role)
    {
        return CommandPlan.Create(EngineeringSystemId.From(Guid.NewGuid()), "test", "test", 100, "pct", "test",
            new List<CommandPlanStep> { new("test.control", "set", 50, "pct", deviceRole: role, sequence: 0) });
    }

    private static CommandPlan CreatePlanAllRoles()
    {
        return CommandPlan.Create(EngineeringSystemId.From(Guid.NewGuid()), "test", "test", 100, "pct", "test",
            new List<CommandPlanStep>
            {
                new("control.heater-output", "set", 50, "pct", deviceRole: SupplyHeater, sequence: 0),
                new("control.fan-speed", "set", 50, "pct", deviceRole: SupplyFan, sequence: 1),
                new("control.fan-speed", "set", 50, "pct", deviceRole: ExhaustFan, sequence: 2),
                new("control.damper-position", "set", 50, "pct", deviceRole: SupplyDamper, sequence: 3)
            });
    }

    private static EngineeringSystem CreateSystem()
    {
        return EngineeringSystem.Create(BuildingId.From(Guid.NewGuid()), "Test", SystemType.Ventilation);
    }
}
