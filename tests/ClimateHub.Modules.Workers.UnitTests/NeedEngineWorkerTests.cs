using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Workers.UnitTests;

public class NeedEngineWorkerTests
{
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());

    [Fact]
    public void Need_CooldownExpiry_ClearsCooldown()
    {
        var need = CreateNeed();
        need.SetCooldown(TimeSpan.FromMinutes(5));

        Assert.NotNull(need.CooldownUntil);

        need.ClearCooldown();
        Assert.Null(need.CooldownUntil);
    }

    [Fact]
    public void Need_StaleData_BlocksNeed()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        need.Block("ENVIRONMENT_DATA_STALE");
        Assert.Equal(NeedStatus.Blocked, need.Status);
    }

    [Fact]
    public void Need_Create_InitialStateIsDetected()
    {
        var need = CreateNeed();
        Assert.Equal(NeedStatus.Detected, need.Status);
    }

    [Fact]
    public void Need_BeginPlanning_TransitionsCorrectly()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        Assert.Equal(NeedStatus.Planning, need.Status);
        Assert.Equal(1, need.PlanningAttemptCount);
    }

    [Fact]
    public void Need_MarkPlanned_SetsPlannedStatus()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        Assert.Equal(NeedStatus.Planned, need.Status);
    }

    [Fact]
    public void Need_MarkExecuting_IncrementsAttemptCount()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        var initial = need.CommandAttemptCount;
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        Assert.Equal(initial + 1, need.CommandAttemptCount);
    }

    [Fact]
    public void Need_Satisfy_SetsStatus()
    {
        var need = CreateNeed();
        need.BeginPlanning();
        need.MarkPlanned(DeviceId.From(Guid.NewGuid()), "control.fan-speed");
        need.MarkExecuting(new CommandId(Guid.NewGuid()));
        need.WaitForEffect(TimeSpan.FromMinutes(2));
        need.Satisfy();
        Assert.Equal(NeedStatus.Satisfied, need.Status);
        Assert.NotNull(need.ResolvedAt);
    }

    private Need CreateNeed()
    {
        return Need.Create(_buildingId, _roomId, NeedType.TemperatureHeating,
            NeedSeverity.Medium, 18, 22, 21, 21, 0, "temperature");
    }
}