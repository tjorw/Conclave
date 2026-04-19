using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Events;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class TicketRevokedHandler(ISessionRegistrationRepository sessionRegistrationRepository)
    : IDomainEventHandler<TicketRevoked>
{
    public async Task Handle(TicketRevoked notification, CancellationToken ct)
    {
        var registrations = await sessionRegistrationRepository
            .GetAllConfirmedByTicketIdAsync(notification.TicketId, ct);

        foreach (var registration in registrations)
            registration.Cancel();

        if (registrations.Count > 0)
            await sessionRegistrationRepository.SaveAsync(ct);
    }
}
