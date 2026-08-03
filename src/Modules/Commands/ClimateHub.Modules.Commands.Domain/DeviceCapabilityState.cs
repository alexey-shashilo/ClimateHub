using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.Commands.Domain;

public class DeviceCapabilityState
{
    public long Id { get; set; }
    public DeviceId DeviceId { get; set; }
    public string CapabilityCode { get; set; } = "";
    public string? DesiredValue { get; set; }
    public DateTimeOffset? DesiredAt { get; set; }
    public CommandId? DesiredByCommandId { get; set; }
    public string? ReportedValue { get; set; }
    public DateTimeOffset? ReportedAt { get; set; }
    public CommandId? ReportedByCommandId { get; set; }
    public string Quality { get; set; } = "unknown";
    public uint Version { get; set; }
}

public record CommandSummaryDto(
    CommandId Id,
    string Status,
    DeviceId DeviceId,
    string CapabilityCode,
    string Operation,
    string ParametersJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? LastErrorCode);