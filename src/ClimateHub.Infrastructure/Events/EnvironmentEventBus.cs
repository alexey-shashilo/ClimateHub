using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Infrastructure.Events;

public class EnvironmentUpdatedEvent : EventArgs
{
    public required RoomId RoomId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string EventType { get; init; } = "environment.updated";
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string? PayloadJson { get; init; }
}

public class EnvironmentEventBus
{
    private readonly object _lock = new();
    private event EventHandler<EnvironmentUpdatedEvent>? _onUpdate;

    public event EventHandler<EnvironmentUpdatedEvent> OnUpdate
    {
        add { lock (_lock) { _onUpdate += value; } }
        remove { lock (_lock) { _onUpdate -= value; } }
    }

    public void Publish(EnvironmentUpdatedEvent evt)
    {
        EventHandler<EnvironmentUpdatedEvent>? handler;
        lock (_lock) { handler = _onUpdate; }
        handler?.Invoke(this, evt);
    }
}
