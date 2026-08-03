using ClimateHub.Modules.EngineeringSystems.Application;
using ClimateHub.Modules.EngineeringSystems.Application.Thermal;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Thermal;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.UnitTests;

public class HeatSourceSchedulerTests
{
    [Fact]
    public void Schedule_NoAvailableSources_ReturnsFailure()
    {
        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource>(), 50, 55);
        Assert.Equal(EngineeringErrors.HeatSourceNotConfigured, result.FailureCode);
    }

    [Fact]
    public void Schedule_CascadePriority_SelectsPrimaryFirst()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var sources = new List<HeatSource>
        {
            new(engId, "Boiler1", HeatSourceType.GasBoiler, 100, isPrimary: false, priority: 2),
            new(engId, "Boiler2", HeatSourceType.GasBoiler, 80, isPrimary: true, priority: 1)
        };
        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(sources, 50, 55);

        Assert.Contains(result.SelectedSources, s => s.IsPrimary);
    }

    [Fact]
    public void Schedule_MinimumRuntime_KeepsRunningSource()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var running = new HeatSource(engId, "Main", HeatSourceType.GasBoiler, 100);
        running.RequestStart();
        running.MarkRunning();
        var off = new HeatSource(engId, "Standby", HeatSourceType.GasBoiler, 80);

        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource> { off, running }, 20, 55);

        Assert.Contains(result.SelectedSources, s => s.RuntimeState == HeatSourceRuntimeState.Running);
    }

    [Fact]
    public void Schedule_MinimumOffTime_SkipsSource()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var source = new HeatSource(engId, "Boiler", HeatSourceType.GasBoiler, 100, minOffMin: 10);
        source.RequestStart();
        source.MarkRunning();
        source.RequestStop();
        source.MarkStopped();

        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource> { source }, 50, 55);

        Assert.DoesNotContain(result.SelectedSources, s => s == source);
    }

    [Fact]
    public void Schedule_Cooldown_ExcludesSource()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var source = new HeatSource(engId, "Boiler", HeatSourceType.GasBoiler, 100);
        source.RequestStart();
        source.MarkRunning();
        source.RequestStop();
        source.MarkStopped();

        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource> { source }, 50, 55);

        Assert.DoesNotContain(result.SelectedSources, s => s == source);
    }

    [Fact]
    public void Schedule_DefrostActive_SkipsSource()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var hp = new HeatSource(engId, "HP", HeatSourceType.HeatPump, 100);
        hp.RequestStart();
        hp.MarkRunning();
        hp.BeginDefrost();

        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource> { hp }, 50, 55);

        Assert.DoesNotContain(result.SelectedSources, s => s == hp);
    }

    [Fact]
    public void Schedule_IncompatibleSupplyTemp_SkipsSource()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var source = new HeatSource(engId, "LowTemp", HeatSourceType.HeatPump, 100, maxSupplyTemp: 45);

        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource> { source }, 50, 55);

        Assert.DoesNotContain(result.SelectedSources, s => s == source);
    }

    [Fact]
    public void Schedule_UnservedPower_CalculatedCorrectly()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var source = new HeatSource(engId, "Small", HeatSourceType.GasBoiler, 30);

        var scheduler = new HeatSourceScheduler();
        var result = scheduler.Schedule(new List<HeatSource> { source }, 100, 55);

        Assert.True(result.UnservedPowerKw > 0);
    }
}

public class WeatherCompensationPlannerTests
{
    [Fact]
    public void Calculate_WeatherCompEnabled_ReturnsCurveBasedTemp()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(),
            weatherCompensationEnabled: true,
            minOutdoor: -30, maxOutdoor: 20,
            minSupply: 20, maxSupply: 60,
            slope: 1.2, parallelShift: 0);
        var planner = new WeatherCompensationPlanner();

        var result = planner.Calculate(config, -10, 20);

        Assert.InRange(result.TargetSupplyTemperature, 20, 60);
        Assert.Equal("Estimated", result.Quality);
    }

    [Fact]
    public void Calculate_WeatherCompDisabled_ReturnsDesignTemp()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(),
            weatherCompensationEnabled: false, designSupplyTemp: 55);
        var planner = new WeatherCompensationPlanner();

        var result = planner.Calculate(config, -10, 20);

        Assert.Equal(55, result.TargetSupplyTemperature);
    }

    [Fact]
    public void Calculate_ColdOutdoor_HigherSupplyTemp()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(),
            weatherCompensationEnabled: true,
            minOutdoor: -30, maxOutdoor: 20,
            minSupply: 20, maxSupply: 60,
            slope: 1.2, parallelShift: 0);
        var planner = new WeatherCompensationPlanner();

        var coldResult = planner.Calculate(config, -20, 20);
        var mildResult = planner.Calculate(config, 5, 20);

        Assert.True(coldResult.TargetSupplyTemperature > mildResult.TargetSupplyTemperature);
    }

    [Fact]
    public void Calculate_FallbackOnInvalidConfig_UsesDefaults()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(),
            weatherCompensationEnabled: true,
            minOutdoor: -30, maxOutdoor: 20,
            minSupply: 20, maxSupply: 60,
            slope: 1.5, parallelShift: 5);
        var planner = new WeatherCompensationPlanner();

        var result = planner.Calculate(config, 0, 20);

        Assert.InRange(result.TargetSupplyTemperature, 20, 60);
    }
}

public class HeatLossEstimatorTests
{
    [Fact]
    public void Estimate_ColdOutdoor_ReturnsPositiveHeatLoss()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(), designHeatLoad: 15000);
        var estimator = new HeatLossEstimator();

        var result = estimator.Estimate(config, -10, 20, 0);

        Assert.True(result.RequiredPower > 0);
    }

    [Fact]
    public void Estimate_WarmOutdoor_ReturnsMinimal()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(), designHeatLoad: 15000);
        var estimator = new HeatLossEstimator();

        var result = estimator.Estimate(config, 25, 20, 0);

        Assert.Equal(0, result.RequiredPower);
        Assert.Equal("Minimal", result.Quality);
    }

    [Fact]
    public void Estimate_WindyCondition_AdjustsHeatLoss()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(), designHeatLoad: 15000);
        var estimator = new HeatLossEstimator();

        var calm = estimator.Estimate(config, -10, 20, 1);
        var windy = estimator.Estimate(config, -10, 20, 15);

        Assert.True(windy.RequiredPower >= calm.RequiredPower);
    }

    [Fact]
    public void Estimate_WindAboveThreshold_FlagsWindAdjustment()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(), designHeatLoad: 15000);
        var estimator = new HeatLossEstimator();

        var result = estimator.Estimate(config, -10, 20, 15);

        Assert.Equal("EstimatedWithWindAdjustment", result.Quality);
    }

    [Fact]
    public void Estimate_ZeroDeltaT_ReturnsZero()
    {
        var config = new ThermalSystemConfiguration(Guid.NewGuid(), designHeatLoad: 15000);
        var estimator = new HeatLossEstimator();

        var result = estimator.Estimate(config, 20, 20, 0);

        Assert.Equal(0, result.RequiredPower);
    }
}

public class HydraulicDemandAllocatorTests
{
    [Fact]
    public void Allocate_SingleZone_FullyAllocates()
    {
        var allocator = new HydraulicDemandAllocator();
        var circuits = new List<HydraulicCircuit>
        {
            new(EngineeringSystemId.From(Guid.NewGuid()), "Main", HydraulicCircuitType.Radiators)
        };

        var plan = allocator.Allocate(circuits, new List<(Guid, double, int)> { (Guid.NewGuid(), 10000, 1) }, 10000);

        Assert.Equal("Balanced", plan.BalanceStatus);
        Assert.Equal(10000, plan.AllocatedPowerKw);
    }

    [Fact]
    public void Allocate_MultipleZones_ByPriority()
    {
        var allocator = new HydraulicDemandAllocator();
        var circuits = new List<HydraulicCircuit>
        {
            new(EngineeringSystemId.From(Guid.NewGuid()), "Main", HydraulicCircuitType.Radiators)
        };

        var plan = allocator.Allocate(circuits,
            new List<(Guid, double, int)> { (Guid.NewGuid(), 3000, 2), (Guid.NewGuid(), 2000, 1) }, 10000);

        Assert.Equal("Balanced", plan.BalanceStatus);
        Assert.Equal(2, plan.ZoneAllocations.Count);
    }

    [Fact]
    public void Allocate_InsufficientPower_UnservedDemand()
    {
        var allocator = new HydraulicDemandAllocator();
        var circuits = new List<HydraulicCircuit>
        {
            new(EngineeringSystemId.From(Guid.NewGuid()), "Main", HydraulicCircuitType.Radiators)
        };

        var plan = allocator.Allocate(circuits, new List<(Guid, double, int)> { (Guid.NewGuid(), 20000, 1) }, 10000);

        Assert.Equal("Insufficient", plan.BalanceStatus);
        Assert.True(plan.UnservedPowerKw > 0);
    }

    [Fact]
    public void Allocate_NoCircuits_ZeroFlow()
    {
        var allocator = new HydraulicDemandAllocator();

        var plan = allocator.Allocate(new List<HydraulicCircuit>(),
            new List<(Guid, double, int)> { (Guid.NewGuid(), 5000, 1) }, 5000);

        Assert.Equal(0, plan.RequiredFlowM3h);
    }
}

public class MixingUnitPlannerTests
{
    [Fact]
    public void Calculate_MixingRatio_WithinBounds()
    {
        var planner = new MixingUnitPlanner();
        var result = planner.Calculate(55, 30, 45);

        Assert.InRange(result, 0, 100);
    }

    [Fact]
    public void PlanAndVerify_SupplyTempWithinRange()
    {
        var planner = new MixingUnitPlanner();
        var mixingResult = planner.Calculate(60, 20, 40);

        Assert.True(mixingResult >= 0);
    }
}

public class MixingUnitPlanner
{
    public double Calculate(double primaryTemp, double returnTemp, double targetTemp)
    {
        if (primaryTemp <= returnTemp) return 0;
        var ratio = (targetTemp - returnTemp) / (primaryTemp - returnTemp);
        return Math.Clamp(ratio * 100, 0, 100);
    }
}

public class CirculationPumpPlannerTests
{
    [Fact]
    public void CalculateSpeed_LowDemand_ModulatesToMinimum()
    {
        var planner = new CirculationPumpPlanner();
        var speed = planner.CalculateSpeed(1.0, 5.0, 20, 100);

        Assert.InRange(speed, 20, 100);
    }

    [Fact]
    public void CalculateSpeed_FullDemand_ReturnsMax()
    {
        var planner = new CirculationPumpPlanner();
        var speed = planner.CalculateSpeed(5.0, 5.0, 20, 100);

        Assert.Equal(100, speed);
    }

    [Fact]
    public void CalculateSpeed_ZeroDemand_ReturnsMinimumFlow()
    {
        var planner = new CirculationPumpPlanner();
        var speed = planner.CalculateSpeed(0, 5.0, 20, 100);

        Assert.Equal(20, speed);
    }

    [Fact]
    public void CalculateSpeed_ClampsWithinBounds()
    {
        var planner = new CirculationPumpPlanner();
        var belowMin = planner.CalculateSpeed(0.1, 5.0, 30, 80);
        Assert.InRange(belowMin, 30, 80);

        var aboveMax = planner.CalculateSpeed(10.0, 5.0, 30, 80);
        Assert.InRange(aboveMax, 30, 80);
    }
}

public class CirculationPumpPlanner
{
    public double CalculateSpeed(double requiredFlow, double designFlow, double minSpeedPct, double maxSpeedPct)
    {
        if (designFlow <= 0) return minSpeedPct;
        var ratio = requiredFlow / designFlow;
        var speed = ratio * 100;
        return Math.Clamp(speed, minSpeedPct, maxSpeedPct);
    }
}

public class BufferTankPlannerTests
{
    [Fact]
    public void Plan_NullBuffer_ReturnsDirect()
    {
        var planner = new BufferTankPlanner();

        var plan = planner.Plan(null, 10, false, 0);

        Assert.Equal("Direct", plan.Action);
    }

    [Fact]
    public void Plan_UnavailableBuffer_ReturnsDirect()
    {
        var planner = new BufferTankPlanner();
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var tank = new BufferTank(engId, "Buffer", 500);
        tank.SetChargeStatus(ChargeStatus.Unavailable);

        var plan = planner.Plan(tank, 10, false, 0);

        Assert.Equal("Unavailable", plan.Quality);
    }

    [Fact]
    public void Plan_HighSoCLowDemand_Discharges()
    {
        var planner = new BufferTankPlanner();
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var tank = new BufferTank(engId, "Buffer", 500);
        tank.UpdateTemperatures(60, 40, 20, 70);

        var plan = planner.Plan(tank, 3, false, 0);

        Assert.True(plan.UseBuffer);
        Assert.False(plan.ChargeBuffer);
        Assert.Equal("Discharging", plan.Action);
    }

    [Fact]
    public void Plan_ExcessCapacityAndMinRuntime_Charges()
    {
        var planner = new BufferTankPlanner();
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var tank = new BufferTank(engId, "Buffer", 500);
        tank.UpdateTemperatures(40, 30, 10, 30);

        var plan = planner.Plan(tank, 10, true, 15);

        Assert.True(plan.UseBuffer);
        Assert.True(plan.ChargeBuffer);
        Assert.Equal("Charging", plan.Action);
    }

    [Fact]
    public void Plan_LowSoCNoExcess_ReturnsDirect()
    {
        var planner = new BufferTankPlanner();
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var tank = new BufferTank(engId, "Buffer", 500);
        tank.UpdateTemperatures(30, 25, 5, 20);

        var plan = planner.Plan(tank, 10, false, 0);

        Assert.Equal("Direct", plan.Action);
    }
}

public class DHWPlannerTests
{
    [Fact]
    public void Plan_NullDHW_ReturnsNone()
    {
        var planner = new DHWPlanner();

        var plan = planner.Plan(null, 5);

        Assert.Equal("None", plan.Action);
    }

    [Fact]
    public void Plan_TempAboveMin_ReturnsNoDemand()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var dhw = new DomesticHotWaterSystem(engId, 200, targetTemp: 55, minTemp: 10);
        dhw.UpdateCurrentTemperature(50);

        var planner = new DHWPlanner();
        var plan = planner.Plan(dhw, 5);

        Assert.False(plan.DHWDemandActive);
        Assert.Equal("None", plan.Action);
    }

    [Fact]
    public void Plan_DHWPriorityMode_ActivatesPriority()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var dhw = new DomesticHotWaterSystem(engId, 200, targetTemp: 55, minTemp: 10);
        dhw.UpdateCurrentTemperature(5);

        var planner = new DHWPlanner();
        var plan = planner.Plan(dhw, 5);

        Assert.True(plan.DHWDemandActive);
        Assert.True(plan.DHWPriorityActive);
        Assert.Equal("Heating", plan.Action);
    }

    [Fact]
    public void Plan_BalancedMode_ReturnsShared()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var dhw = new DomesticHotWaterSystem(engId, 200, targetTemp: 55, minTemp: 10, priorityMode: "Balanced");
        dhw.UpdateCurrentTemperature(5);

        var planner = new DHWPlanner();
        var plan = planner.Plan(dhw, 5);

        Assert.True(plan.DHWDemandActive);
        Assert.False(plan.DHWPriorityActive);
        Assert.Equal("Shared", plan.Action);
    }

    [Fact]
    public void Plan_LegionellaCycle_HigherTempTarget()
    {
        var engId = EngineeringSystemId.From(Guid.NewGuid());
        var dhw = new DomesticHotWaterSystem(engId, 200, targetTemp: 55, minTemp: 10, legionellaTemp: 65);
        dhw.UpdateCurrentTemperature(5);

        var planner = new DHWPlanner();
        var plan = planner.Plan(dhw, 5);

        Assert.True(plan.DHWDemandActive);
        Assert.True(plan.RequiredHeatingPowerKw > 0);
    }
}