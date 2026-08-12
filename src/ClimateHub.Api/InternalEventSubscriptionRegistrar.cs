using System.Text.Json;
using ClimateHub.Infrastructure.InternalEvents;
using ClimateHub.Modules.Needs.Infrastructure;
using ClimateHub.SharedKernel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClimateHub.Api;

/// <summary>
/// Registers internal event dispatch subscriptions for the in-process aggregate engines
/// (Need evaluation and Climate planning). Subscribers are resolved lazily per event scope
/// so scoped repositories remain valid. Events are delivered through the persistent
/// internal-event outbox so producers (e.g. Device Gateway) and consumers (API) may run
/// in separate processes.
/// </summary>
public class InternalEventSubscriptionRegistrar : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InternalEventSubscriptionRegistrar> _logger;

    public InternalEventSubscriptionRegistrar(
        IServiceScopeFactory scopeFactory,
        ILogger<InternalEventSubscriptionRegistrar> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var root = _scopeFactory.CreateScope();
        var dispatcher = root.ServiceProvider.GetRequiredService<InternalEventDispatcher>();

        dispatcher.Subscribe("environment.state.changed", async (envelope, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<RoomEnvironmentStateChangedHandler>();
            var evt = ParseEnvironmentStateChanged(envelope);
            if (evt is not null)
            {
                await handler.HandleAsync(evt, ct);
            }
        });

        _logger.LogInformation("Internal event subscriptions registered");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static RoomEnvironmentStateChangedEvent? ParseEnvironmentStateChanged(InternalEventEnvelope envelope)
    {
        if (envelope.RoomId is null) return null;

        var buildingId = envelope.BuildingId.HasValue ? BuildingId.From(envelope.BuildingId.Value) : BuildingId.From(Guid.Empty);
        var changed = new List<string>();
        var measuredAt = envelope.OccurredAt;
        var receivedAt = envelope.OccurredAt;

        if (!string.IsNullOrWhiteSpace(envelope.Payload))
        {
            try
            {
                using var doc = JsonDocument.Parse(envelope.Payload);
                if (doc.RootElement.TryGetProperty("changedParameters", out var cp) && cp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in cp.EnumerateArray())
                        changed.Add(item.GetString() ?? string.Empty);
                }
                else if (doc.RootElement.TryGetProperty("changed_parameters", out var cps) && cps.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in cps.EnumerateArray())
                        changed.Add(item.GetString() ?? string.Empty);
                }
                if (doc.RootElement.TryGetProperty("measuredAt", out var m) &&
                    DateTimeOffset.TryParse(m.GetString(), out var parsedM))
                    measuredAt = parsedM;
                if (doc.RootElement.TryGetProperty("measured_at", out var ms) &&
                    DateTimeOffset.TryParse(ms.GetString(), out var parsedMs))
                    measuredAt = parsedMs;
                if (doc.RootElement.TryGetProperty("receivedAt", out var r) &&
                    DateTimeOffset.TryParse(r.GetString(), out var parsedR))
                    receivedAt = parsedR;
                if (doc.RootElement.TryGetProperty("received_at", out var rs) &&
                    DateTimeOffset.TryParse(rs.GetString(), out var parsedRs))
                    receivedAt = parsedRs;
            }
            catch (JsonException)
            {
                // ignore malformed payload
            }
        }

        return new RoomEnvironmentStateChangedEvent(
            envelope.EventId.ToString(),
            buildingId,
            RoomId.From(envelope.RoomId.Value),
            changed,
            measuredAt,
            receivedAt,
            DeviceId.From(Guid.Empty),
            envelope.OccurredAt,
            envelope.CorrelationId,
            envelope.CausationId);
    }
}
