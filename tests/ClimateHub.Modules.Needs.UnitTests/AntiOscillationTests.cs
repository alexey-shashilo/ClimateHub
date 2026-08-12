using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.UnitTests;

public class AntiOscillationTests
{
    [Fact]
    public void MarkExecuting_IncrementsCommandAttemptCount()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        int initial = need.CommandAttemptCount;
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        Assert.Equal(initial + 1, need.CommandAttemptCount);
    }

    [Fact]
    public void CommandAttemptCount_StartsAtZero()
    {
        var need = CreateNeed();
        Assert.Equal(0, need.CommandAttemptCount);
    }

    [Fact]
    public void PlanningAttemptCount_IncrementsOnBeginPlanning()
    {
        var need = CreateNeed();
        Assert.Equal(0, need.PlanningAttemptCount);
        need.BeginPlanning();
        Assert.Equal(1, need.PlanningAttemptCount);
    }

    [Fact]
    public void Cooldown_ShouldSetAndClear()
    {
        var need = CreateNeed();
        need.SetCooldown(TimeSpan.FromMinutes(5));
        Assert.NotNull(need.CooldownUntil);
        need.ClearCooldown();
        Assert.Null(need.CooldownUntil);
    }

    [Fact]
    public void Create_Co2Reduction_ShouldSetCorrectType()
    {
        var need = Need.Create(
            BuildingId.From(Guid.NewGuid()),
            RoomId.From(Guid.NewGuid()),
            NeedType.Co2Reduction, NeedSeverity.High, 0, 1000, 700, 700, 300, "co2",
            ControlMode.Automatic);
        Assert.Equal(NeedType.Co2Reduction, need.Type);
        Assert.Equal(NeedSeverity.High, need.Severity);
    }

    [Fact]
    public void MarkExecuting_MultipleTimes_ShouldIncrementCount()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        int first = need.CommandAttemptCount;
        need.ClearActiveCommand();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        Assert.Equal(first + 1, need.CommandAttemptCount);
    }

    [Fact]
    public void EffectEvaluationDueAt_ShouldBeNullByDefault()
    {
        var need = CreateNeed();
        Assert.Null(need.EffectEvaluationDueAt);
    }

    [Fact]
    public void SetEffectEvaluationDue_ShouldSetValue()
    {
        var need = CreateNeed();
        need.SetEffectEvaluationDue(TimeSpan.FromSeconds(30));
        Assert.NotNull(need.EffectEvaluationDueAt);
    }

    [Fact]
    public void ClearEffectEvaluationDue_ShouldClear()
    {
        var need = CreateNeed();
        need.SetEffectEvaluationDue(TimeSpan.FromSeconds(30));
        need.ClearEffectEvaluationDue();
        Assert.Null(need.EffectEvaluationDueAt);
    }

    [Fact]
    public void ViolationSince_ShouldSetOnce()
    {
        var need = CreateNeed();
        need.SetViolationSince();
        var first = need.ViolationSince;
        need.SetViolationSince();
        Assert.Equal(first, need.ViolationSince);
    }

    [Fact]
    public void ClearViolationSince_ShouldClear()
    {
        var need = CreateNeed();
        need.SetViolationSince();
        need.ClearViolationSince();
        Assert.Null(need.ViolationSince);
    }

    [Fact]
    public void RecordMeaningfulImprovement_ShouldSetTimestamp()
    {
        var need = CreateNeed();
        need.RecordMeaningfulImprovement();
        Assert.NotNull(need.LastMeaningfulImprovementAt);
    }

    private static Need CreateNeed()
    {
        return Need.Create(
            BuildingId.From(Guid.NewGuid()),
            RoomId.From(Guid.NewGuid()),
            NeedType.Co2Reduction, NeedSeverity.High, 0, 1000, 700, 700, 300,
            "co2", ControlMode.Automatic);
    }
}
