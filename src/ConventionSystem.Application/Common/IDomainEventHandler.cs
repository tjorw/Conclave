using ConventionSystem.Domain.Common;
using MediatR;

namespace ConventionSystem.Application.Common;

public interface IDomainEventHandler<TEvent> : INotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
}
