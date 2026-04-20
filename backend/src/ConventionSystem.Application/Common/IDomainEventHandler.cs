using ConventionSystem.Domain.Common;

namespace ConventionSystem.Application.Common;

public interface IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken ct);
}
