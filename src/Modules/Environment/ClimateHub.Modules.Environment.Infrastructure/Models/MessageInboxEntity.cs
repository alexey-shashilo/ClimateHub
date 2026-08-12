namespace ClimateHub.Modules.Environment.Infrastructure.Models;

public class MessageInboxEntity
{
    public long Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string MessageIdStr { get; set; } = string.Empty;
    public string DeviceIdStr { get; set; } = string.Empty;
    public string? BootIdStr { get; set; }
    public long? SequenceNumber { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public string Status { get; set; } = "Received";
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ErrorCode { get; set; }
    public int AttemptCount { get; set; }
}
