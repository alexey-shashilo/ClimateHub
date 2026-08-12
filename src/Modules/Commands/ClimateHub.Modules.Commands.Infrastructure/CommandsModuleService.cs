using ClimateHub.Modules.Commands.Application;
using ClimateHub.Modules.Commands.Contracts;
using ClimateHub.Modules.Commands.Domain;
using ClimateHub.Modules.Commands.Domain.Repositories;

namespace ClimateHub.Modules.Commands.Infrastructure;

public class CommandsModuleService(
    CreateCommandHandler createHandler,
    CancelCommandHandler cancelHandler,
    ICommandRepository commandRepo) : ICommandsModule
{
    public async Task<CreateCommandResultDto> CreateAsync(CreateCommandRequestDto request, CancellationToken ct = default)
    {
        var req = new CreateCommandRequest
        {
            BuildingId = request.BuildingId,
            RoomId = request.RoomId,
            DeviceId = request.DeviceId,
            CapabilityCode = request.CapabilityCode,
            Operation = request.Operation,
            ParametersJson = request.ParametersJson,
            CreatedBy = request.CreatedBy,
            CorrelationId = request.CorrelationId,
        };

        var result = await createHandler.HandleAsync(req, ct);
        return new CreateCommandResultDto(new CommandIdDto(result.Id.Value), result.Status.ToString());
    }

    public async Task<CommandStatusDto?> GetStatusAsync(CommandIdDto commandId, CancellationToken ct = default)
    {
        var cmd = await commandRepo.GetByIdAsync(new CommandId(commandId.Value), ct);
        if (cmd is null) return null;
        return new CommandStatusDto(
            new CommandIdDto(cmd.Id.Value),
            cmd.Status.ToString(),
            cmd.LastErrorCode,
            cmd.CreatedAt,
            cmd.CompletedAt);
    }

    public async Task<CancelCommandResultDto> CancelAsync(CommandIdDto commandId, CancellationToken ct = default)
    {
        try
        {
            await cancelHandler.HandleAsync(new CommandId(commandId.Value), ct);
            return new CancelCommandResultDto(true, null);
        }
        catch (Exception ex)
        {
            return new CancelCommandResultDto(false, ex.GetType().Name);
        }
    }
}
