using ClimateHub.Modules.Building.Contracts;
using ClimateHub.Modules.Commands.Application;
using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.SharedKernel.Primitives;
using NSubstitute;

namespace ClimateHub.Modules.Commands.UnitTests;

public class CreateCommandHandlerTests
{
    private readonly BuildingId _buildingId = BuildingId.From(Guid.NewGuid());
    private readonly DeviceId _deviceId = DeviceId.From(Guid.NewGuid());
    private readonly RoomId _roomId = RoomId.From(Guid.NewGuid());
    private readonly CapabilityDefinitionDto _capDef = new(
        "temperature.setpoint", "numeric", "double", "celsius", true,
        new[] { "set" }, 0, 100, 1, false, false);

    [Fact]
    public async Task HandleAsync_ValidParameters_CreatesCommand()
    {
        var (handler, cmdRepo, outboxRepo, _, _, _, _) = CreateHandler();

        var request = new CreateCommandRequest
        {
            BuildingId = _buildingId.Value.ToString(),
            DeviceId = _deviceId.Value.ToString(),
            RoomId = _roomId.Value.ToString(),
            CapabilityCode = "temperature.setpoint",
            Operation = "set",
            ParametersJson = "{\"value\": 22}",
            Priority = "normal"
        };

        var result = await handler.HandleAsync(request);

        Assert.NotNull(result);
        Assert.Equal(CommandStatus.Queued, result.Status);
        await cmdRepo.Received(1).AddAsync(Arg.Any<Command>(), Arg.Any<CancellationToken>());
        await outboxRepo.Received(1).AddAsync(Arg.Any<CommandOutbox>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidBuildingId_Throws()
    {
        var (handler, _, _, _, _, _, _) = CreateHandler();

        var request = new CreateCommandRequest
        {
            BuildingId = "not-a-guid",
            DeviceId = _deviceId.Value.ToString(),
            CapabilityCode = "temperature.setpoint",
            Operation = "set"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_InvalidDeviceId_Throws()
    {
        var (handler, _, _, _, _, _, _) = CreateHandler();

        var request = new CreateCommandRequest
        {
            BuildingId = _buildingId.Value.ToString(),
            DeviceId = "not-a-guid",
            CapabilityCode = "temperature.setpoint",
            Operation = "set"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_UnknownCapability_Throws()
    {
        var (handler, _, _, _, _, _, catalog) = CreateHandler();
        catalog.GetDefinitionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((CapabilityDefinitionDto?)null);

        var request = new CreateCommandRequest
        {
            BuildingId = _buildingId.Value.ToString(),
            DeviceId = _deviceId.Value.ToString(),
            CapabilityCode = "unknown.capability",
            Operation = "set"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_DeviceNotFound_Throws()
    {
        var (handler, _, _, _, devicesModule, _, _) = CreateHandler();
        devicesModule.GetDeviceAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>())
            .Returns((DeviceInfo?)null);

        var request = new CreateCommandRequest
        {
            BuildingId = _buildingId.Value.ToString(),
            DeviceId = _deviceId.Value.ToString(),
            CapabilityCode = "temperature.setpoint",
            Operation = "set"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(request));
    }

    [Fact]
    public async Task HandleAsync_UnsupportedOperation_Throws()
    {
        var (handler, _, _, _, _, _, catalog) = CreateHandler();
        catalog.GetDefinitionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CapabilityDefinitionDto(
                "temperature.setpoint", "numeric", "double", "celsius", true,
                new[] { "read" }, 0, 100, 1, false, false));

        var request = new CreateCommandRequest
        {
            BuildingId = _buildingId.Value.ToString(),
            DeviceId = _deviceId.Value.ToString(),
            CapabilityCode = "temperature.setpoint",
            Operation = "write"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(request));
    }

    private (CreateCommandHandler Handler, ICommandRepository CmdRepo, ICommandOutboxRepository OutboxRepo,
        IDeviceCapabilityStateRepository StateRepo, IDevicesModule DevicesModule, IBuildingModule BuildingModule,
        ICapabilityCatalog Catalog) CreateHandler()
    {
        var cmdRepo = Substitute.For<ICommandRepository>();
        var outboxRepo = Substitute.For<ICommandOutboxRepository>();
        var stateRepo = Substitute.For<IDeviceCapabilityStateRepository>();
        var devicesModule = Substitute.For<IDevicesModule>();
        var buildingModule = Substitute.For<IBuildingModule>();
        var catalog = Substitute.For<ICapabilityCatalog>();

        catalog.GetDefinitionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_capDef);
        devicesModule.GetDeviceAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>())
            .Returns(new DeviceInfo(_deviceId, "Test Device", "Thermostat"));
        devicesModule.GetDeviceRoomAssignmentAsync(Arg.Any<DeviceId>(), Arg.Any<CancellationToken>()).Returns(_roomId);
        buildingModule.BuildingExistsAsync(Arg.Any<BuildingId>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = new CreateCommandHandler(cmdRepo, outboxRepo, stateRepo, devicesModule, buildingModule, catalog);
        return (handler, cmdRepo, outboxRepo, stateRepo, devicesModule, buildingModule, catalog);
    }
}

public class CancelCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_QueuedCommand_CancelsSuccessfully()
    {
        var (handler, cmdRepo, _) = CreateCancelHandler();
        var cmdId = CommandId.New();
        var cmd = CreateQueuedCommand();
        cmdRepo.GetByIdAsync(cmdId, Arg.Any<CancellationToken>()).Returns(cmd);

        var result = await handler.HandleAsync(cmdId);

        Assert.Equal(CommandStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task HandleAsync_CommandNotFound_Throws()
    {
        var (handler, cmdRepo, _) = CreateCancelHandler();
        cmdRepo.GetByIdAsync(Arg.Any<CommandId>(), Arg.Any<CancellationToken>())
            .Returns((Command?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.HandleAsync(CommandId.New()));
    }

    [Fact]
    public async Task HandleAsync_AlreadySucceeded_Throws()
    {
        var (handler, cmdRepo, _) = CreateCancelHandler();
        var cmdId = CommandId.New();
        var cmd = CreateCompletedCommand();
        cmdRepo.GetByIdAsync(cmdId, Arg.Any<CancellationToken>()).Returns(cmd);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmdId));
    }

    private (CancelCommandHandler Handler, ICommandRepository CmdRepo, ICapabilityCatalog Catalog) CreateCancelHandler()
    {
        var cmdRepo = Substitute.For<ICommandRepository>();
        var outboxRepo = Substitute.For<ICommandOutboxRepository>();
        var catalog = Substitute.For<ICapabilityCatalog>();
        catalog.GetDefinitionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CapabilityDefinitionDto(
                "temperature.setpoint", "numeric", "double", "celsius", true,
                Array.Empty<string>(), null, null, 1, false, false));

        var handler = new CancelCommandHandler(cmdRepo, outboxRepo, catalog);
        return (handler, cmdRepo, catalog);
    }

    private static Command CreateQueuedCommand()
    {
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}");
        cmd.Validate();
        cmd.Queue();
        return cmd;
    }

    private static Command CreateCompletedCommand()
    {
        var cmd = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}");
        cmd.Validate();
        cmd.Queue();
        cmd.MarkPublished();
        cmd.Acknowledge();
        cmd.StartExecution();
        cmd.CompleteSuccessfully();
        return cmd;
    }
}

public class CommandStatusTransitionsTests
{
    [Fact]
    public void ValidTransition_CreatedToValidated_ReturnsTrue()
    {
        Assert.True(CommandTransitions.IsValid(CommandStatus.Created, CommandStatus.Validated));
    }

    [Fact]
    public void ValidTransition_ValidatedToQueued_ReturnsTrue()
    {
        Assert.True(CommandTransitions.IsValid(CommandStatus.Validated, CommandStatus.Queued));
    }

    [Fact]
    public void InvalidTransition_SkippingStates_ReturnsFalse()
    {
        Assert.False(CommandTransitions.IsValid(CommandStatus.Created, CommandStatus.Published));
    }

    [Fact]
    public void TerminalState_NoTransitionsOut()
    {
        Assert.False(CommandTransitions.IsValid(CommandStatus.Succeeded, CommandStatus.Cancelled));
        Assert.False(CommandTransitions.IsValid(CommandStatus.Failed, CommandStatus.Created));
        Assert.False(CommandTransitions.IsValid(CommandStatus.Rejected, CommandStatus.Queued));
    }

    [Fact]
    public void DuplicateCommand_PreventionViaClientRequestId()
    {
        var clientId = Guid.NewGuid();
        var cmd1 = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}",
            clientRequestId: clientId);
        var cmd2 = Command.Create(CommandId.New(), BuildingId.From(Guid.NewGuid()), DeviceId.From(Guid.NewGuid()), "test.cap", "set", "{}",
            clientRequestId: clientId);

        Assert.NotEqual(cmd1.Id, cmd2.Id);
        Assert.Equal(cmd1.ClientRequestId, cmd2.ClientRequestId);
    }
}
