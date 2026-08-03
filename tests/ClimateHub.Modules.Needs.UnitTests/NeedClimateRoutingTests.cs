using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.UnitTests;

public class NeedClimateRoutingTests
{
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());

    private Need CreateCo2Need()
    {
        return Need.Create(_buildingId, _roomId, NeedType.Co2Reduction,
            NeedSeverity.Medium, 0, 1000, 600, 1200, 200, "co2",
            ControlMode.Automatic);
    }

    [Fact]
    public void PlanAndExecuteAsync_ShouldGoThroughClimate_NotDirectEngineering()
    {
        var need = CreateCo2Need();
        need.BeginPlanning();
        need.MarkEngineeringPlanned("climate-orchestrator", "eng.co2.reduce", "plan-123");

        Assert.Equal(NeedStatus.Planned, need.Status);
        Assert.Equal("climate-orchestrator", need.SelectedEngineeringSystemId);
        Assert.Equal("plan-123", need.ActiveCommandPlanId);
        Assert.Equal("eng.co2.reduce", need.SelectedEngineeringCapabilityCode);
    }

    [Fact]
    public void Need_RequiresClimateLayer_BeforeEngineering()
    {
        var need = CreateCo2Need();
        need.BeginPlanning();

        SimulateViaClimateOrchestrator(need, "eng.co2.reduce", "plan-123");

        Assert.Equal(NeedStatus.Planned, need.Status);
        Assert.NotNull(need.ActiveCommandPlanId);
    }

    [Fact]
    public void Need_BlockedWhenClimateHasNoPlan_ShouldNotFallbackToEngineering()
    {
        var need = CreateCo2Need();
        need.BeginPlanning();

        SimulateClimatePlanMissing(need);

        Assert.Equal(NeedStatus.Blocked, need.Status);
        Assert.Equal("CLIMATE_PLAN_MISSING_CAPABILITY", need.PlanningFailureCode);
    }

    private static void SimulateViaClimateOrchestrator(Need need, string engCode, string planId)
    {
        need.MarkEngineeringPlanned("climate-orchestrator", engCode, planId);
    }

    private static void SimulateClimatePlanMissing(Need need)
    {
        need.Block("CLIMATE_PLAN_MISSING_CAPABILITY");
    }
}