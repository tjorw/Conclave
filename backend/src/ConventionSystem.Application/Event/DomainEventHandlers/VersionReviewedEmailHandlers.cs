using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Event.DomainEventHandlers;

public sealed class EventApprovedEmailHandler(
    IPersonRepository personRepository,
    IConventionRepository conventionRepository,
    IEmailService emailService)
    : IDomainEventHandler<EventApproved>
{
    public async Task Handle(EventApproved notification, CancellationToken ct)
    {
        var organiser = await personRepository.GetByIdAsync(notification.LeadOrganiserId, ct);
        if (organiser is null) return;

        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null) return;

        await emailService.SendEventApprovedAsync(organiser.Email, organiser.Name, notification.EventTitle, convention.Id.Value, ct);
    }
}

public sealed class EventRejectedEmailHandler(
    IPersonRepository personRepository,
    IConventionRepository conventionRepository,
    IEmailService emailService)
    : IDomainEventHandler<EventRejected>
{
    public async Task Handle(EventRejected notification, CancellationToken ct)
    {
        var organiser = await personRepository.GetByIdAsync(notification.LeadOrganiserId, ct);
        if (organiser is null) return;

        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null) return;

        await emailService.SendEventRejectedAsync(
            organiser.Email, organiser.Name, notification.EventTitle, notification.RejectionComment, convention.Id.Value, ct);
    }
}
