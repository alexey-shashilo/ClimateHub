using ClimateHub.Modules.EngineeringSystems.Application;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Humidification;
using ClimateHub.SharedKernel.Primitives;
using static ClimateHub.Modules.EngineeringSystems.Domain.DeviceRole;

namespace ClimateHub.Modules.EngineeringSystems.UnitTests;

public class HumidityCalculationEngineTests
{
    private readonly HumidityCalculationEngine _engine = new();

    [Fact]
    public void CalculateAbsoluteHumidity_StandardConditions_ReturnsPositiveValue()
    {
        var ah = _engine.CalculateAbsoluteHumidity(22, 50);
        Assert.True(ah > 0);
        Assert.True(ah < 20);
    }

    [Fact]
    public void CalculateAbsoluteHumidity_DryAir_ReturnsLowValue()
    {
        var dry = _engine.CalculateAbsoluteHumidity(22, 10);
        var humid = _engine.CalculateAbsoluteHumidity(22, 90);
        Assert.True(dry < humid);
    }

    [Fact]
    public void CalculateRequiredMoisture_Deficit_ReturnsPositive()
    {
        var required = _engine.CalculateRequiredMoisture(1000, 20, 50, 18, 22);
        Assert.True(required > 0);
    }

    [Fact]
    public void CalculateRequiredMoisture_NoDeficit_ReturnsZero()
    {
        var required = _engine.CalculateRequiredMoisture(1000, 50, 50, 22, 22);
        Assert.Equal(0, required);
    }

    [Fact]
    public void SteamToPower_ConvertsCorrectly()
    {
        var power = _engine.SteamToPower(10);
        Assert.Equal(7.5, power);
    }

    [Fact]
    public void WaterToSteam_ConvertsCorrectly()
    {
        var steam = _engine.WaterToSteam(10);
        Assert.Equal(9.5, steam);
    }

    [Fact]
    public void CalculateAbsoluteHumidity_ZeroPercent_ReturnsNearZero()
    {
        var ah = _engine.CalculateAbsoluteHumidity(22, 0);
        Assert.Equal(0, ah, 5);
    }
}

public class CondensationProtectionPlannerTests
{
    [Fact]
    public void Evaluate_ProtectionDisabled_ReturnsNoRisk()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), condensationProtection: false);
        var planner = new CondensationProtectionPlanner();

        var result = planner.Evaluate(22, 50, null, null, config);

        Assert.Equal(CondensationRisk.None, result.Risk);
        Assert.False(result.BlockHumidification);
    }

    [Fact]
    public void Evaluate_CriticalDelta_BlocksHumidification()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), condensationProtection: true, dewPointDelta: 3);
        var planner = new CondensationProtectionPlanner();

        var result = planner.Evaluate(20, 95, 19.5, 5, config);

        Assert.Equal(CondensationRisk.Critical, result.Risk);
        Assert.True(result.BlockHumidification);
    }

    [Fact]
    public void Evaluate_SafeDelta_DoesNotBlock()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), condensationProtection: true, dewPointDelta: 3);
        var planner = new CondensationProtectionPlanner();

        var result = planner.Evaluate(22, 40, 18, 5, config);

        Assert.DoesNotContain(new[] { CondensationRisk.Critical, CondensationRisk.High }, r => r == result.Risk);
        Assert.False(result.BlockHumidification);
    }

    [Fact]
    public void Evaluate_CalculateDewPoint_Accurate()
    {
        var dewPoint = CondensationProtectionPlanner.CalculateDewPoint(22, 50);
        Assert.InRange(dewPoint, 10, 15);
    }

    [Fact]
    public void Evaluate_InvalidRH_ReturnsNaN()
    {
        var dewPoint = CondensationProtectionPlanner.CalculateDewPoint(22, 0);
        Assert.Equal(double.NaN, dewPoint);
    }

    [Fact]
    public void Evaluate_OutOfRangeTemp_ReturnsNaN()
    {
        var dewPoint = CondensationProtectionPlanner.CalculateDewPoint(-50, 50);
        Assert.Equal(double.NaN, dewPoint);
    }

    [Fact]
    public void Evaluate_MaxSafeRH_ReasonableValue()
    {
        var maxRh = CondensationProtectionPlanner.CalculateMaxSafeRh(22, 16, 3);
        Assert.InRange(maxRh, 30, 80);
    }
}

public class HumidificationDemandAggregatorTests
{
    private readonly RoomId _roomA = RoomId.From(Guid.NewGuid());
    private readonly RoomId _roomB = RoomId.From(Guid.NewGuid());

    [Fact]
    public void Aggregate_MultiZone_ReturnsClampedToMax()
    {
        var agg = new HumidificationDemandAggregator();
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), maxCapacity: 100);

        var demands = new List<HumidificationDemand>
        {
            new(Guid.NewGuid(), _roomA, 30, 50, 10, 60, "Critical"),
            new(Guid.NewGuid(), _roomB, 35, 50, 12, 50, "High")
        };

        var total = agg.AggregateDemand(demands, config);
        Assert.Equal(100, total);
    }

    [Fact]
    public void Aggregate_EmptyDemands_ReturnsZero()
    {
        var agg = new HumidificationDemandAggregator();
        var config = new HumidificationSystemConfiguration(Guid.NewGuid());

        var total = agg.AggregateDemand(new List<HumidificationDemand>(), config);

        Assert.Equal(0, total);
    }

    [Fact]
    public void Aggregate_SortsBySeverityThenPriority()
    {
        var agg = new HumidificationDemandAggregator();
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), maxCapacity: 100);

        var lowDemand = new HumidificationDemand(Guid.NewGuid(), _roomA, 30, 50, 10, 30, "Low", priority: 1);
        var highDemand = new HumidificationDemand(Guid.NewGuid(), _roomB, 30, 50, 10, 30, "Critical", priority: 2);

        var total = agg.AggregateDemand(new List<HumidificationDemand> { lowDemand, highDemand }, config);

        Assert.Equal(60, total);
    }

    [Fact]
    public void ResolveZone_MatchingRoom_ReturnsZone()
    {
        var agg = new HumidificationDemandAggregator();
        var zone = new HumidificationZone(Guid.NewGuid(), "Zone1");
        var room = new HumidificationZoneRoom(_roomA);
        zone.AddRoom(room);

        var resolved = agg.ResolveZone(new List<HumidificationZone> { zone }, _roomA);

        Assert.NotNull(resolved);
    }

    [Fact]
    public void ResolveZone_NoMatch_ReturnsNull()
    {
        var agg = new HumidificationDemandAggregator();
        var zone = new HumidificationZone(Guid.NewGuid(), "Zone1");
        var room = new HumidificationZoneRoom(_roomA);
        zone.AddRoom(room);

        var resolved = agg.ResolveZone(new List<HumidificationZone> { zone }, _roomB);

        Assert.Null(resolved);
    }
}

public class HumidificationStrategyEngineTests
{
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());

    [Fact]
    public void PlanHumidification_SteamModeNoGenerator_ReturnsFailure()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), mode: HumidificationMode.Steam, hasSteamGen: false);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var result = engine.PlanHumidification(config, 30, 50, 22, 18, 1000, 5, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(EngineeringErrors.SteamGeneratorNotAvailable, result.FailureCode);
    }

    [Fact]
    public void PlanHumidification_CondensationBlocks_ReturnsFailure()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), condensationProtection: true, dewPointDelta: 1);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var result = engine.PlanHumidification(config, 30, 95, 20, 18, 1000, 5, 19.5);

        Assert.False(result.IsSuccess);
        Assert.Equal(EngineeringErrors.CondensationRiskDetected, result.FailureCode);
    }

    [Fact]
    public void PlanHumidification_SteamMode_ReturnsSteamSteps()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), mode: HumidificationMode.Steam, hasSteamGen: true);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var result = engine.PlanHumidification(config, 30, 50, 22, 18, 1000, 5, null);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Steps, s => s.DeviceRole == SteamGenerator);
    }

    [Fact]
    public void PlanHumidification_SteamModeWithWaterTreatment_IncludesROAndUV()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), mode: HumidificationMode.Steam, hasSteamGen: true, hasWaterTreatment: true);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var result = engine.PlanHumidification(config, 30, 50, 22, 18, 1000, 5, null);

        Assert.Contains(result.Steps, s => s.DeviceRole == ROSystem);
        Assert.Contains(result.Steps, s => s.DeviceRole == UVSterilizer);
    }

    [Fact]
    public void PlanHumidification_AdiabaticMode_ReturnsNozzleSteps()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), mode: HumidificationMode.Adiabatic);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var result = engine.PlanHumidification(config, 30, 50, 22, 18, 1000, 5, null, humidifierType: HumidifierType.HighPressureNozzle);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Steps, s => s.DeviceRole == NozzlePump);
        Assert.Contains(result.Steps, s => s.DeviceRole == NozzleValve);
    }

    [Fact]
    public void PlanHumidification_NoMoistureNeeded_ReturnsZeroCapacity()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), mode: HumidificationMode.Steam, hasSteamGen: true);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var result = engine.PlanHumidification(config, 60, 50, 22, 22, 1000, 5, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.RequiredCapacityKgH);
    }

    [Fact]
    public void BuildSanitaryCycleSteps_FlushAndSterilization_IncludesSteps()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid(), hasUV: true, legionellaTemp: 65);
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var steps = engine.BuildSanitaryCycleSteps(config, true, true);

        Assert.Contains(steps, s => s.DeviceRole == FlushValve);
        Assert.Contains(steps, s => s.DeviceRole == DrainValve);
        Assert.Contains(steps, s => s.DeviceRole == UVSterilizer);
        Assert.Contains(steps, s => s.CapabilityCode == "control.water-heater");
    }

    [Fact]
    public void BuildSanitaryCycleSteps_NoFlushNoSterilization_ReturnsEmpty()
    {
        var config = new HumidificationSystemConfiguration(Guid.NewGuid());
        var engine = new HumidificationStrategyEngine(new CondensationProtectionPlanner(), new HumidityCalculationEngine());

        var steps = engine.BuildSanitaryCycleSteps(config, false, false);

        Assert.Empty(steps);
    }
}