using ClimateHub.Modules.Climate.Application.DependencyGraph;
using ClimateHub.Modules.Climate.Domain.ClimateGoals;
using ClimateHub.Modules.Climate.Domain.ClimatePlans;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Climate.UnitTests;

public class DependencyGraphValidatorTests
{
    private readonly ClimateDependencyGraphValidator _validator = new();
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());
    private readonly ClimateGoalId _goalId = ClimateGoalId.New();

    [Fact]
    public void ValidGraph_ShouldReturnIsValid()
    {
        var plan = CreatePlan();
        var subPlanA = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        var subPlanB = new EngineeringSubPlan("eng.airflow.increase", 1, "Medium", 50, 1);
        plan.AddSubPlan(subPlanA);
        plan.AddSubPlan(subPlanB);
        plan.AddDependency(new ClimatePlanDependency(subPlanA.Id, subPlanB.Id,
            DependencyType.FinishToStart, true, "Heating before ventilation"));

        var result = _validator.Validate(plan);
        Assert.True(result.IsValid);
        Assert.False(result.HasCycle);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void SelfDependency_ShouldReturnError()
    {
        var plan = CreatePlan();
        var subPlan = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        plan.AddSubPlan(subPlan);
        plan.AddDependency(new ClimatePlanDependency(subPlan.Id, subPlan.Id));

        var result = _validator.Validate(plan);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Self-dependency"));
    }

    [Fact]
    public void Cycle_ShouldBeDetected()
    {
        var plan = CreatePlan();
        var a = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        var b = new EngineeringSubPlan("eng.airflow.increase", 1, "Medium", 50, 1);
        plan.AddSubPlan(a);
        plan.AddSubPlan(b);
        plan.AddDependency(new ClimatePlanDependency(a.Id, b.Id));
        plan.AddDependency(new ClimatePlanDependency(b.Id, a.Id));

        var result = _validator.Validate(plan);
        Assert.False(result.IsValid);
        Assert.True(result.HasCycle);
    }

    [Fact]
    public void MissingNode_ShouldReturnError()
    {
        var plan = CreatePlan();
        var subPlan = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        plan.AddSubPlan(subPlan);
        plan.AddDependency(new ClimatePlanDependency(subPlan.Id, Guid.NewGuid()));

        var result = _validator.Validate(plan);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void NoDependencies_AllSubPlansReady()
    {
        var plan = CreatePlan();
        var a = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        var b = new EngineeringSubPlan("eng.airflow.increase", 1, "Medium", 50, 1);
        plan.AddSubPlan(a);
        plan.AddSubPlan(b);

        var result = _validator.Validate(plan);
        Assert.True(result.IsValid);
        Assert.Equal(2, result.ReadySubPlanIds.Count);
    }

    [Fact]
    public void DuplicateDependency_ShouldReturnError()
    {
        var plan = CreatePlan();
        var a = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        var b = new EngineeringSubPlan("eng.airflow.increase", 1, "Medium", 50, 1);
        plan.AddSubPlan(a);
        plan.AddSubPlan(b);
        plan.AddDependency(new ClimatePlanDependency(a.Id, b.Id));
        plan.AddDependency(new ClimatePlanDependency(a.Id, b.Id));

        var result = _validator.Validate(plan);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate"));
    }

    [Fact]
    public void SafetyGateDependency_BlocksReady()
    {
        var plan = CreatePlan();
        var a = new EngineeringSubPlan("eng.temperature.increase", 1, "High", 50, 0);
        var b = new EngineeringSubPlan("eng.airflow.increase", 1, "Medium", 50, 1);
        plan.AddSubPlan(a);
        plan.AddSubPlan(b);
        plan.AddDependency(new ClimatePlanDependency(a.Id, b.Id, DependencyType.SafetyGate, true));

        var result = _validator.Validate(plan);
        Assert.True(result.IsValid);
        Assert.Contains(a.Id, result.ReadySubPlanIds);
    }

    private ClimatePlan CreatePlan()
    {
        return ClimatePlan.Create(_goalId, _buildingId, _roomId, StrategyProfile.Balanced);
    }
}