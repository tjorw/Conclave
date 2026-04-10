using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class SessionDeactivatedHandler(ISessionRegistrationRepository sessionRegistrationRepository)
    : IDomainEventHandler<SessionDeactivated>
{
    public async Task Handle(SessionDeactivated notification, CancellationToken ct)
    {
        var registrations = await sessionRegistrationRepository
            .GetAllConfirmedBySessionIdAsync(notification.SessionId, ct);

        foreach (var registration in registrations)
            registration.Cancel();

        if (registrations.Count > 0)
            await sessionRegistrationRepository.SaveAsync(ct);
    }
}
