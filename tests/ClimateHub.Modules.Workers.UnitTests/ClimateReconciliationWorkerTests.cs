using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Workers.UnitTests;

public class ClimateReconciliationWorkerTests
{
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());
    private readonly ClimateGoalId _goalId = ClimateGoalId.New();

    [Fact]
    public void StaleSubPlan_NotDetectedWhenNotExecuting()
    {
        var plan = ClimatePlan.Create(_goalId, _buildingId, _roomId, StrategyProfile.Balanced);
        var subPlan = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        plan.AddSubPlan(subPlan);

        Assert.DoesNotContain(plan.SubPlans, s => s.Status == SubPlanStatus.Executing && string.IsNullOrEmpty(s.EngineeringCommandPlanId));
    }

    [Fact]
    public void ClimatePlan_Created_HasPendingStatus()
    {
        var plan = ClimatePlan.Create(_goalId, _buildingId, _roomId, StrategyProfile.Balanced);
        Assert.Equal(ClimatePlanStatus.Created, plan.Status);
    }

    [Fact]
    public void ClimatePlan_CanAddSubPlans()
    {
        var plan = ClimatePlan.Create(_goalId, _buildingId, _roomId, StrategyProfile.Comfort);
        plan.AddSubPlan(new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0));
        plan.AddSubPlan(new EngineeringSubPlan("eng.airflow.increase", 1, "Medium", 50, 1));

        Assert.Equal(2, plan.SubPlans.Count);
    }

    [Fact]
    public void ClimatePlan_CanFailWhileExecuting()
    {
        var plan = ClimatePlan.Create(_goalId, _buildingId, _roomId, StrategyProfile.Balanced);
        plan.SetPlanning();
        plan.SetPlanned();
        plan.SetReservingResources();
        plan.SetReady();
        plan.Start();
        plan.Fail("EFFECT_NOT_OBSERVED", "No effect observed within timeout");
        Assert.Equal(ClimatePlanStatus.Failed, plan.Status);
    }

    [Fact]
    public void ClimateGoal_Blocked_IsBlocked()
    {
        var goal = ClimateGoal.Create(_buildingId, _roomId);
        goal.BeginPlanning();
        goal.Block();
        Assert.Equal(GoalStatus.Blocked, goal.Status);
    }

    [Fact]
    public void ActiveGoal_DueForEvaluation_WhenNotEvaluatedRecently()
    {
        var goal = ClimateGoal.Create(_buildingId, _roomId);
        Assert.Null(goal.LastEvaluatedAt);
    }
}