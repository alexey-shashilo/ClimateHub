using System.Text.Json;
using ClimateHub.Infrastructure.Events;
using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClimateHub.DeviceGateway.Services;

public class CommandEventConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandEventConsumer> _logger;

    public CommandEventConsumer(IServiceScopeFactory scopeFactory, ILogger<CommandEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(string topic, string payloadJson, CancellationToken ct = default)
    {
        // Parse topic: climate-hub/v1/{buildingId}/{deviceId}/command/{eventType}
        var parts = topic.Split('/');
        if (parts.Length < 6 || parts[0] != "climate-hub" || parts[1] != "v1" || parts[4] != "command")
            return;

        var eventType = string.Join("/", parts.Skip(5));
        if (eventType is not ("ack" or "progress" or "result" or "error"))
            return;

        using var scope = _scopeFactory.CreateScope();
        var inboxRepo = scope.ServiceProvider.GetRequiredService<ICommandInboxRepository>();
        var cmdRepo = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
        var stateRepo = scope.ServiceProvider.GetRequiredService<IDeviceCapabilityStateRepository>();
        var eventBus = scope.ServiceProvider.GetService<EnvironmentEventBus>();

        CommandEventEnvelope? envelope;
        try { envelope = JsonSerializer.Deserialize<CommandEventEnvelope>(payloadJson); }
        catch { _logger.LogWarning("Invalid command event JSON from topic {Topic}", topic); return; }

        if (envelope is null) return;

        // Deduplication via Inbox
        if (await inboxRepo.IsDuplicateAsync("mqtt", envelope.MessageId, ct))
        {
            _logger.LogDebug("Duplicate command event skipped: {MessageId}", envelope.MessageId);
            return;
        }

        if (!Guid.TryParse(envelope.CommandId, out var cmdGuid))
        { _logger.LogWarning("Invalid commandId in event"); return; }
        var commandId = CommandId.From(cmdGuid);

        var command = await cmdRepo.GetByIdAsync(commandId, ct);
        if (command is null)
        { _logger.LogWarning("Command {CommandId} not found for event", commandId); return; }

        try
        {
            switch (eventType)
            {
                case "ack":
                    command.Acknowledge();
                    break;
                case "progress":
                    // Progress doesn't change status if already in executing
                    if (command.Status == CommandStatus.Acknowledged)
                        command.StartExecution();
                    _logger.LogInformation("Command {CommandId} progress", commandId);
                    break;
                case "result":
                    await HandleResultAsync(command, envelope, stateRepo, ct);
                    break;
                case "error":
                    var errCode = envelope.Payload?.GetProperty("errorCode").GetString() ?? "UNKNOWN_ERROR";
                    var errMsg = envelope.Payload?.GetProperty("errorMessage").GetString() ?? "";
                    command.CompleteWithFailure(errCode, errMsg);
                    break;
            }

            await cmdRepo.UpdateAsync(command, ct);
            await inboxRepo.MarkProcessedAsync("mqtt", envelope.MessageId, commandId, eventType, ct);

            // Publish SSE event
            eventBus?.Publish(new EnvironmentUpdatedEvent
            {
                RoomId = command.RoomId ?? SharedKernel.Primitives.RoomId.From(Guid.NewGuid()),
                Timestamp = DateTimeOffset.UtcNow,
                EventType = $"command.{command.Status.ToString().ToLowerInvariant()}"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid transition for command {CommandId} status {Status}", commandId, command.Status);
        }
    }

    private static async Task HandleResultAsync(Command command, CommandEventEnvelope envelope,
        IDeviceCapabilityStateRepository stateRepo, CancellationToken ct)
    {
        var status = envelope.Payload?.GetProperty("status").GetString();
        if (status == "succeeded")
        {
            command.CompleteSuccessfully();
            // Update reported state
            if (envelope.Payload?.TryGetProperty("reportedState", out var reported) == true)
            {
                var reportedJson = reported.GetRawText();
                await stateRepo.UpsertReportedAsync(command.DeviceId, command.CapabilityCode,
                    reportedJson, command.Id, ct);
            }
        }
        else
        {
            var errCode = envelope.Payload?.GetProperty("errorCode").GetString() ?? "EXECUTION_FAILED";
            var errMsg = envelope.Payload?.GetProperty("errorMessage").GetString() ?? "";
            command.CompleteWithFailure(errCode, errMsg);
        }
    }
}

public class CommandEventEnvelope
{
    public string MessageId { get; set; } = "";
    public string MessageType { get; set; } = "";
    public string ProtocolVersion { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string CommandId { get; set; } = "";
    public int AttemptNumber { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public JsonElement? Payload { get; set; }
}
