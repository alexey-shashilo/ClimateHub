using System.Text.Json;
using ClimateHub.Modules.Building.Contracts;
using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.SharedKernel.Domain;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Commands.Application;

public class CreateCommandRequest
{
    public string BuildingId { get; set; } = "";
    public string? RoomId { get; set; }
    public string DeviceId { get; set; } = "";
    public string CapabilityCode { get; set; } = "";
    public string Operation { get; set; } = "";
    public string ParametersJson { get; set; } = "{}";
    public string Priority { get; set; } = "normal";
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? ClientRequestId { get; set; }
}

public class CreateCommandHandler(
    ICommandRepository cmdRepo,
    ICommandOutboxRepository outboxRepo,
    IDeviceCapabilityStateRepository stateRepo,
    IDevicesModule devicesModule,
    IBuildingModule buildingModule,
    ICapabilityCatalog capabilityCatalog)
{
    public async Task<Command> HandleAsync(CreateCommandRequest request, CancellationToken ct = default)
    {
        if (!Guid.TryParse(request.BuildingId, out var bg) || !Guid.TryParse(request.DeviceId, out var dg))
            throw new ArgumentException("Invalid buildingId or deviceId");
        var buildingId = BuildingId.From(bg);
        var deviceId = DeviceId.From(dg);
        RoomId? roomId = request.RoomId is not null && Guid.TryParse(request.RoomId, out var rg) ? RoomId.From(rg) : null;

        var def = await capabilityCatalog.GetDefinitionAsync(request.CapabilityCode, ct);
        if (def is null) throw new InvalidOperationException($"Unknown capability: {request.CapabilityCode}");
        if (def.SupportedOperations is not null && !def.SupportedOperations.Contains(request.Operation))
            throw new ArgumentException($"Operation '{request.Operation}' not supported for capability '{request.CapabilityCode}'");

        var device = await devicesModule.GetDeviceAsync(deviceId, ct);
        if (device is null) throw new KeyNotFoundException("DEVICE_NOT_FOUND");

        if (roomId.HasValue)
        {
            var assignment = await devicesModule.GetDeviceRoomAssignmentAsync(deviceId, ct);
            if (assignment != roomId) throw new InvalidOperationException("DEVICE_NOT_ASSIGNED");
        }
        if (!await buildingModule.BuildingExistsAsync(buildingId, ct))
            throw new KeyNotFoundException("BUILDING_NOT_FOUND");

        var cmdId = CommandId.New();
        var command = Command.Create(cmdId, buildingId, deviceId,
            request.CapabilityCode, request.Operation, request.ParametersJson,
            CommandPriority.Normal, request.ExpiresAt, request.CreatedBy,
            request.CorrelationId, request.ClientRequestId, roomId);
        command.Validate();
        command.Queue();

        var mqttTopic = $"climate-hub/v1/{request.BuildingId}/{request.DeviceId}/command/execute";
        var envelope = new
        {
            messageId = Guid.NewGuid().ToString(),
            messageType = "command.execute",
            protocolVersion = "1.0",
            buildingId = request.BuildingId,
            deviceId = request.DeviceId,
            commandId = cmdId.ToString(),
            attemptNumber = 1,
            createdAt = DateTimeOffset.UtcNow,
            expiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(2),
            payload = new
            {
                capabilityCode = request.CapabilityCode,
                operation = request.Operation,
                contractVersion = def.ContractVersion.ToString(),
                parameters = JsonDocument.Parse(request.ParametersJson).RootElement
            }
        };
        var outboxPayload = JsonSerializer.Serialize(envelope);

        var outbox = new CommandOutbox
        {
            CommandId = cmdId,
            DeviceId = deviceId,
            Topic = mqttTopic,
            Payload = outboxPayload,
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
            AvailableAt = DateTimeOffset.UtcNow
        };

        await stateRepo.UpsertDesiredAsync(deviceId, request.CapabilityCode, request.ParametersJson, cmdId, ct);
        await cmdRepo.AddAsync(command, ct);
        await outboxRepo.AddAsync(outbox, ct);

        command.PublishDomainEvent(new CommandCreatedEvent(cmdId, deviceId, request.CapabilityCode, request.Operation));
        return command;
    }
}

public class CancelCommandHandler(ICommandRepository cmdRepo, ICommandOutboxRepository outboxRepo, ICapabilityCatalog capabilityCatalog)
{
    public async Task<Command> HandleAsync(CommandId commandId, CancellationToken ct = default)
    {
        var cmd = await cmdRepo.GetByIdAsync(commandId, ct);
        if (cmd is null) throw new KeyNotFoundException("COMMAND_NOT_FOUND");
        var def = await capabilityCatalog.GetDefinitionAsync(cmd.CapabilityCode, ct);
        if (def is null) throw new InvalidOperationException($"Unknown capability: {cmd.CapabilityCode}");
        if (!CommandTransitions.IsValid(cmd.Status, CommandStatus.Cancelled))
            throw new InvalidOperationException("COMMAND_CANNOT_BE_CANCELLED");

        if (def.SupportsCancellation && cmd.Status == CommandStatus.Published)
        {
            cmd.RequestCancellation();
            var topic = $"climate-hub/v1/{cmd.BuildingId}/{cmd.DeviceId}/command/cancel";
            await outboxRepo.AddAsync(new CommandOutbox
            {
                CommandId = cmd.Id,
                DeviceId = cmd.DeviceId,
                Topic = topic,
                Status = "Pending",
                CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            }, ct);
        }
        else if (cmd.Status is CommandStatus.Queued or CommandStatus.Created or CommandStatus.Validated) { cmd.Cancel(); }
        else throw new InvalidOperationException("COMMAND_CANNOT_BE_CANCELLED");
        await cmdRepo.UpdateAsync(cmd, ct);
        return cmd;
    }
}
