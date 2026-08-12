using ClimateHub.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClimateHub.Infrastructure.Observability;

public class DomainEventInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return await base.SavingChangesAsync(eventData, result, ct);

        var aggregates = ctx.ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            await dispatcher.DispatchAsync(aggregate.DomainEvents, ct);
            aggregate.ClearDomainEvents();
        }

        return await base.SavingChangesAsync(eventData, result, ct);
    }
}
