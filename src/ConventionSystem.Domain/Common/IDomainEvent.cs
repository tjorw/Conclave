using MediatR;

namespace ConventionSystem.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}
