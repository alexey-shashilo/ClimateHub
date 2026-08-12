using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Commands.Contracts;

public readonly record struct CommandIdDto(Guid Value)
{
    public static CommandIdDto From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}

public record CreateCommandRequestDto(
    string BuildingId,
    string? RoomId,
    string DeviceId,
    string CapabilityCode,
    string Operation,
    string ParametersJson,
    string CreatedBy,
    string? CorrelationId);

public record CreateCommandResultDto(CommandIdDto CommandId, string Status);

public record CommandStatusDto(
    CommandIdDto CommandId,
    string Status,
    string? LastErrorCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public record CancelCommandResultDto(bool Success, string? ErrorCode);

public interface ICommandsModule
{
    Task<CreateCommandResultDto> CreateAsync(CreateCommandRequestDto request, CancellationToken ct = default);
    Task<CommandStatusDto?> GetStatusAsync(CommandIdDto commandId, CancellationToken ct = default);
    Task<CancelCommandResultDto> CancelAsync(CommandIdDto commandId, CancellationToken ct = default);
}
