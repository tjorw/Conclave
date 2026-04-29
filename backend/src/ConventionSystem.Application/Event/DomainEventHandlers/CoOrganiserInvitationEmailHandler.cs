using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Event.DomainEventHandlers;

public sealed class CoOrganiserInvitationEmailHandler(IEmailService emailService)
    : IDomainEventHandler<CoOrganiserInvitationCreated>
{
    public async Task Handle(CoOrganiserInvitationCreated notification, CancellationToken ct)
    {
        await emailService.SendCoOrganiserInvitationAsync(notification.Email, notification.Code, ct);
    }
}
