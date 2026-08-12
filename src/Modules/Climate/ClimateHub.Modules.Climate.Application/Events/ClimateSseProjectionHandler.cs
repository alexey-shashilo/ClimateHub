using ClimateHub.Infrastructure.InternalEvents;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Climate.Application.Events;

public class ClimateSseProjectionHandler
{
    private static readonly HashSet<string> SseEventTypes = new()
    {
        "climate.goal.updated", "climate.goal.satisfied", "climate.goal.blocked", "climate.goal.failed",
        "climate.plan.created", "climate.plan.updated", "climate.plan.executing",
        "climate.plan.waiting-for-effect", "climate.plan.completed", "climate.plan.failed", "climate.plan.cancelled",
        "climate.conflict.detected",
        "climate.resource.reserved", "climate.resource.released",
        "climate.effect.timeout"
    };

    private readonly SseEventLogRepository _sseRepo;
    private readonly ILogger<ClimateSseProjectionHandler> _logger;

    public ClimateSseProjectionHandler(SseEventLogRepository sseRepo, ILogger<ClimateSseProjectionHandler> logger)
    {
        _sseRepo = sseRepo; _logger = logger;
    }

    public bool ShouldProject(string eventType) => SseEventTypes.Contains(eventType);

    public async Task ProjectAsync(InternalEventOutbox eventRow, CancellationToken ct = default)
    {
        if (!ShouldProject(eventRow.EventType)) return;

        try
        {
            await _sseRepo.AppendAsync(
                eventRow.EventId, eventRow.EventType,
                eventRow.BuildingId, eventRow.RoomId,
                eventRow.Payload, eventRow.OccurredAt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SSE projection failed for event {EventId} type {Type}", eventRow.EventId, eventRow.EventType);
        }
    }
}
