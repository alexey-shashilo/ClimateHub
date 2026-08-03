using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Needs.UnitTests;

public class NeedLifecycleTests
{
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());

    [Fact]
    public void Create_ValidParameters_ShouldSetInitialState()
    {
        var need = Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Medium, 18, 22, 21, 21, 0, "temperature");

        Assert.Equal(NeedStatus.Detected, need.Status);
        Assert.Equal(NeedSeverity.Medium, need.Severity);
        Assert.Equal(_roomId, need.RoomId);
    }

    [Fact]
    public void BeginPlanning_WhenDetected_ShouldTransitionToPlanning()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        Assert.Equal(NeedStatus.Planning, need.Status);
    }

    [Fact]
    public void BeginPlanning_WhenAlreadyPlanning_ShouldThrow()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        Assert.Throws<InvalidOperationException>(() => need.BeginPlanning());
    }

    [Fact]
    public void MarkPlanned_ShouldTransitionToPlanned()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        Assert.Equal(NeedStatus.Planned, need.Status);
    }

    [Fact]
    public void MarkEngineeringPlanned_ShouldSetEngineeringIds()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkEngineeringPlanned("eng-heating-1", "temperature.increase", "plan-123");
        Assert.Equal(NeedStatus.Planned, need.Status);
        Assert.Equal("eng-heating-1", need.SelectedEngineeringSystemId);
        Assert.Equal("plan-123", need.ActiveCommandPlanId);
    }

    [Fact]
    public void MarkExecuting_ShouldTransitionToExecuting()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        Assert.Equal(NeedStatus.Executing, need.Status);
    }

    [Fact]
    public void Satisfy_ShouldTransitionToSatisfied()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        need.WaitForEffect(TimeSpan.FromSeconds(30));
        need.Satisfy();
        Assert.Equal(NeedStatus.Satisfied, need.Status);
        Assert.NotNull(need.ResolvedAt);
    }

    [Fact]
    public void Block_ShouldTransitionToBlockedWithReason()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Block("CAPABLE_DEVICE_NOT_FOUND");
        Assert.Equal(NeedStatus.Blocked, need.Status);
        Assert.Equal("CAPABLE_DEVICE_NOT_FOUND", need.PlanningFailureCode);
    }

    [Fact]
    public void Cancel_ShouldTransitionToCancelled()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Cancel();
        Assert.Equal(NeedStatus.Cancelled, need.Status);
    }

    [Fact]
    public void Expire_FromBlocked_ShouldTransitionToExpired()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Block("FAILED");
        need.Expire();
        Assert.Equal(NeedStatus.Expired, need.Status);
    }

    [Fact]
    public void Expire_FromSatisfied_ShouldTransitionToExpired()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        need.WaitForEffect(TimeSpan.FromSeconds(30));
        need.Satisfy();
        need.Expire();
        Assert.Equal(NeedStatus.Expired, need.Status);
    }

    [Fact]
    public void TerminalState_ShouldNotAllowFurtherTransitions()
    {
        var need = CreateActiveNeed();
        need.Cancel();
        Assert.Throws<InvalidOperationException>(() => need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed"));
    }

    [Fact]
    public void Create_WithDisabledMode_ShouldSetControlMode()
    {
        var need = Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Low, 18, 22, 21, 21, 0, "temperature",
            ControlMode.Disabled);
        Assert.Equal(ControlMode.Disabled, need.Mode);
    }

    [Fact]
    public void UpdateEvaluation_ShouldUpdateDeviation()
    {
        var need = CreateActiveNeed();
        need.UpdateEvaluation(18.5, 3.5, NeedSeverity.Medium);
        Assert.Equal(18.5, need.CurrentValue);
        Assert.Equal(3.5, need.Deviation);
    }

    [Fact]
    public void UpdateCurrentValue_AndRecalculate_ShouldWork()
    {
        var need = Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Medium, 18, 22, 21, 21, 0, "temperature");
        need.UpdateEvaluation(16, 6, NeedSeverity.High);
        Assert.Equal(6, need.Deviation);
        Assert.Equal(NeedSeverity.High, need.Severity);
    }

    [Fact]
    public void Create_WithMonitorOnlyMode_ShouldBeDefault()
    {
        var need = Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Medium, 18, 22, 21, 21, 0, "temperature");
        Assert.Equal(ControlMode.MonitorOnly, need.Mode);
    }

    [Fact]
    public void Create_WithAutomaticMode_ShouldSetMode()
    {
        var need = Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Medium, 18, 22, 21, 21, 0, "temperature",
            ControlMode.Automatic);
        Assert.Equal(ControlMode.Automatic, need.Mode);
    }

    [Fact]
    public void ClearBlock_ShouldReturnToDetected()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Block("SOME_ERROR");
        need.ClearBlock();
        Assert.Equal(NeedStatus.Detected, need.Status);
        Assert.Null(need.PlanningFailureCode);
    }

    [Fact]
    public void WaitForEffect_ShouldTransitionToWaitingForEffect()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        need.WaitForEffect(TimeSpan.FromMinutes(5));
        Assert.Equal(NeedStatus.WaitingForEffect, need.Status);
        Assert.NotNull(need.EffectEvaluationDueAt);
    }

    [Fact]
    public void ClearActiveCommand_ShouldReturnToDetected()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        need.ClearActiveCommand();
        Assert.Equal(NeedStatus.Detected, need.Status);
    }

    [Fact]
    public void Blocked_CanClearBlockAndTransitionToDetected()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Block("FAILED");
        need.ClearBlock();
        Assert.Equal(NeedStatus.Detected, need.Status);
    }

    [Fact]
    public void Blocked_CanTransitionDirectlyToCancelled()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Block("FAILED");
        need.Cancel();
        Assert.Equal(NeedStatus.Cancelled, need.Status);
    }

    [Fact]
    public void Blocked_CanTransitionToExpired()
    {
        var need = CreateActiveNeed();
        need.BeginPlanning();
        need.Block("FAILED");
        need.Expire();
        Assert.Equal(NeedStatus.Expired, need.Status);
    }

    private Need CreateActiveNeed()
    {
        return Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Medium, 18, 22, 21, 21, 0, "temperature",
            ControlMode.Automatic);
    }
}