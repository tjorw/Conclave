using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Event.DomainEventHandlers;

public sealed class EventApprovedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<EventApproved>
{
    public async Task Handle(EventApproved notification, CancellationToken ct)
    {
        var organiser = await personRepository.GetByIdAsync(notification.LeadOrganiserId, ct);
        if (organiser is null) return;

        await emailService.SendEventApprovedAsync(organiser.Email, organiser.Name, notification.EventTitle, ct);
    }
}

public sealed class EventRejectedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<EventRejected>
{
    public async Task Handle(EventRejected notification, CancellationToken ct)
    {
        var organiser = await personRepository.GetByIdAsync(notification.LeadOrganiserId, ct);
        if (organiser is null) return;

        await emailService.SendEventRejectedAsync(
            organiser.Email, organiser.Name, notification.EventTitle, notification.RejectionComment, ct);
    }
}
