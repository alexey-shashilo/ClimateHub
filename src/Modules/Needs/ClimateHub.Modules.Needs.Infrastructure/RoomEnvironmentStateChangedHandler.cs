using ClimateHub.Infrastructure.Events;
using ClimateHub.Modules.Environment.Contracts;
using ClimateHub.Modules.Needs.Domain;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Modules.Needs.Infrastructure;

public class RoomEnvironmentStateChangedHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoomEnvironmentStateChangedHandler> _logger;

    public RoomEnvironmentStateChangedHandler(IServiceScopeFactory scopeFactory, ILogger<RoomEnvironmentStateChangedHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(RoomEnvironmentStateChangedEvent evt, CancellationToken ct = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var policyReader = scope.ServiceProvider.GetRequiredService<IRoomPolicyReader>();
            var evalService = scope.ServiceProvider.GetRequiredService<NeedEvaluationService>();

            var policy = await policyReader.GetByRoomAsync(evt.RoomId, ct);
            if (policy is null)
                return;

            var relevantParameters = new[] { "temperature", "humidity", "co2", "illuminance" };
            var anyRelevant = evt.ChangedParameters.Any(p =>
                relevantParameters.Contains(p.ToLowerInvariant()));

            if (!anyRelevant)
                return;

            await evalService.EvaluateRoomAsync(evt.RoomId,
                NeedEvaluationTrigger.EnvironmentStateChanged,
                evt.CorrelationId, evt.CausationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle environment state changed event for room {RoomId}", evt.RoomId);
        }
    }
}

public record RoomEnvironmentStateChangedEvent(
    string EventId,
    BuildingId BuildingId,
    RoomId RoomId,
    IReadOnlyCollection<string> ChangedParameters,
    DateTimeOffset MeasuredAt,
    DateTimeOffset ReceivedAt,
    DeviceId SourceDeviceId,
    DateTimeOffset OccurredAt,
    string? CorrelationId = null,
    string? CausationId = null,
    int? EnvironmentStateVersion = null);
