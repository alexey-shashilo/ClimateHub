using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClimateHub.SharedKernel.Domain;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default);
}

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);
            foreach (var handler in handlers)
            {
                try
                {
                    var method = handlerType.GetMethod("HandleAsync");
                    if (method is not null)
                    {
                        await (Task)method.Invoke(handler, [domainEvent, ct])!;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Domain event handler failed for {EventType}", domainEvent.GetType().Name);
                }
            }
        }
    }
}

public static class DomainEventExtensions
{
    public static async Task DispatchAndClearAsync(this IAggregateRoot aggregate,
        IDomainEventDispatcher dispatcher, CancellationToken ct = default)
    {
        if (aggregate.DomainEvents.Count == 0) return;
        try
        {
            await dispatcher.DispatchAsync(aggregate.DomainEvents, ct);
        }
        finally
        {
            aggregate.ClearDomainEvents();
        }
    }
}
