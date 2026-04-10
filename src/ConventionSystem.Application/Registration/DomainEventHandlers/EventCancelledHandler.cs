using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class EventCancelledHandler(
    IEventRepository eventRepository,
    ISessionRegistrationRepository sessionRegistrationRepository)
    : IDomainEventHandler<EventCancelled>
{
    public async Task Handle(EventCancelled notification, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithSessionsAsync(notification.EventId, ct);
        if (ev is null) return;

        var cancelled = false;
        foreach (var session in ev.Sessions)
        {
            var registrations = await sessionRegistrationRepository
                .GetAllConfirmedBySessionIdAsync(session.Id, ct);

            foreach (var registration in registrations)
                registration.Cancel();

            if (registrations.Count > 0)
                cancelled = true;
        }

        if (cancelled)
            await sessionRegistrationRepository.SaveAsync(ct);
    }
}
