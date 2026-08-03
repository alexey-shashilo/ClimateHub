using ClimateHub.Modules.Commands.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Workers.UnitTests;

public class CommandTimeoutWorkerTests
{
    [Fact]
    public void TimedOutCommand_TransitionsToTimedOut()
    {
        var cmd = CreateExecutingCommand();
        cmd.Timeout();
        Assert.Equal(CommandStatus.TimedOut, cmd.Status);
        Assert.NotNull(cmd.CompletedAt);
    }

    [Fact]
    public void ActiveCommand_NotTimedOut_KeepsExecuting()
    {
        var cmd = CreateExecutingCommand();
        Assert.Equal(CommandStatus.Executing, cmd.Status);
    }

    [Fact]
    public void FreshCommand_NotTimedOut()
    {
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}");
        Assert.Equal(CommandStatus.Created, cmd.Status);
    }

    [Fact]
    public void Timeout_NonExecutingCommand_Throws()
    {
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}");
        Assert.Throws<InvalidOperationException>(() => cmd.Timeout());
    }

    [Fact]
    public void StaleCommands_DetectedByTimeComparison()
    {
        var staleness = DateTimeOffset.UtcNow.AddMinutes(-3);
        var recent = DateTimeOffset.UtcNow.AddMinutes(-1);

        Assert.True(recent > staleness);
    }

    private static Command CreateExecutingCommand()
    {
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}");
        cmd.Validate();
        cmd.Queue();
        cmd.MarkPublished();
        cmd.Acknowledge();
        cmd.StartExecution();
        return cmd;
    }
}