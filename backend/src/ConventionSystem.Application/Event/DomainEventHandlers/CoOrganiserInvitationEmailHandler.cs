using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Events;
namespace ConventionSystem.Application.Event.DomainEventHandlers;

public sealed class CoOrganiserInvitationEmailHandler(
    IEventRepository eventRepository,
    IConventionRepository conventionRepository,
    IEmailService emailService)
    : IDomainEventHandler<CoOrganiserInvitationCreated>
{
    public async Task Handle(CoOrganiserInvitationCreated notification, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(notification.EventId, ct);
        var eventTitle = ev?.Title ?? string.Empty;

        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null) return;

        await emailService.SendCoOrganiserInvitationAsync(
            notification.Email,
            string.Empty,
            eventTitle,
            notification.Code,
            convention.Id.Value,
            ct);
    }
}
