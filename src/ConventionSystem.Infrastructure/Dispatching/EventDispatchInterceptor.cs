using ConventionSystem.Domain.Common;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ConventionSystem.Infrastructure.Dispatching;

public sealed class EventDispatchInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ConventionDbContext context)
        {
            var events = context.ChangeTracker
                .Entries<AggregateRoot>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            foreach (var @event in events)
                context.DomainEventLog.Add(DomainEventLogEntry.From(@event));
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ConventionDbContext context)
        {
            var aggregates = context.ChangeTracker
                .Entries<AggregateRoot>()
                .Select(e => e.Entity)
                .Where(a => a.DomainEvents.Any())
                .ToArray();

            await dispatcher.DispatchAndClearEvents(aggregates, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
