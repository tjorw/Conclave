namespace ConventionSystem.Domain.Common;

public interface IDomainEventDispatcher
{
    Task DispatchAndClearEvents(IEnumerable<AggregateRoot> aggregatesWithEvents, CancellationToken cancellationToken = default);
}
