using ConventionSystem.Domain.Common;
using MediatR;

namespace ConventionSystem.Infrastructure.Dispatching;

public sealed class MediatorDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAndClearEvents(
        IEnumerable<AggregateRoot> aggregatesWithEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var aggregate in aggregatesWithEvents)
        {
            var events = aggregate.DomainEvents.ToArray();
            aggregate.ClearDomainEvents();

            foreach (var domainEvent in events)
                await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
