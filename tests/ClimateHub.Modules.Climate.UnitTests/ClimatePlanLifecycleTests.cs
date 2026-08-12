using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.UnitTests;

public class ClimatePlanLifecycleTests
{
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());

    private ClimatePlan CreatePlan()
    {
        var goalId = ClimateGoalId.New();
        return ClimatePlan.Create(goalId, _buildingId, _roomId, StrategyProfile.Comfort);
    }

    [Fact]
    public void Executing_To_WaitingForEffect_ShouldBeValid()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        Assert.Equal(ClimatePlanStatus.Executing, plan.Status);

        plan.WaitForEffect();
        Assert.Equal(ClimatePlanStatus.WaitingForEffect, plan.Status);
        Assert.NotNull(plan.WaitingForEffectAt);
    }

    [Fact]
    public void Executing_To_Completed_Directly_ShouldThrow()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        Assert.Equal(ClimatePlanStatus.Executing, plan.Status);

        Assert.Throws<InvalidOperationException>(() => plan.Complete());
    }

    [Fact]
    public void FullLifecycle_Executing_WaitingForEffect_Completed_ShouldSucceed()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.WaitForEffect();
        plan.Complete();

        Assert.Equal(ClimatePlanStatus.Completed, plan.Status);
        Assert.NotNull(plan.CompletedAt);
        Assert.NotNull(plan.WaitingForEffectAt);
    }

    [Fact]
    public void WaitingForEffect_CanTransitionTo_Failed()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.WaitForEffect();
        plan.Fail("EFFECT_NOT_OBSERVED", "No environmental effect within timeout");

        Assert.Equal(ClimatePlanStatus.Failed, plan.Status);
        Assert.Equal("EFFECT_NOT_OBSERVED", plan.FailureCode);
    }

    [Fact]
    public void WaitingForEffect_CanTransitionTo_PartiallyCompleted()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.WaitForEffect();
        plan.PartiallyComplete();

        Assert.Equal(ClimatePlanStatus.PartiallyCompleted, plan.Status);
    }

    [Fact]
    public void WaitingForEffect_CanTransitionTo_Cancelled()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.WaitForEffect();
        plan.Cancel();

        Assert.Equal(ClimatePlanStatus.Cancelled, plan.Status);
    }

    [Fact]
    public void Executing_To_PartiallyCompleted_ShouldSkipWaitingForEffect_WhenSubPlansPartiallyFail()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.PartiallyComplete();

        Assert.Equal(ClimatePlanStatus.PartiallyCompleted, plan.Status);
    }

    [Fact]
    public void Created_CanTransitionTo_Cancelled()
    {
        var plan = CreatePlan();
        plan.Cancel();
        Assert.Equal(ClimatePlanStatus.Cancelled, plan.Status);
    }

    [Fact]
    public void TerminalState_ShouldNotAllowFurtherTransitions()
    {
        var plan = CreatePlan();
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.WaitForEffect();
        plan.Complete();

        Assert.Throws<InvalidOperationException>(() => plan.Start());
    }

    [Fact]
    public void InvalidTransition_ShouldThrow()
    {
        var plan = CreatePlan();
        Assert.Throws<InvalidOperationException>(() => plan.Complete());
    }
}
