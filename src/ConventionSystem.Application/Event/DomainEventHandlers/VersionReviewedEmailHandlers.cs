using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Event.DomainEventHandlers;

public sealed class VersionApprovedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<VersionApproved>
{
    public async Task Handle(VersionApproved notification, CancellationToken ct)
    {
        var organiser = await personRepository.GetByIdAsync(notification.LeadOrganiserId, ct);
        if (organiser is null) return;

        await emailService.SendEventApprovedAsync(organiser.Email, organiser.Name, notification.EventTitle, ct);
    }
}

public sealed class VersionRejectedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<VersionRejected>
{
    public async Task Handle(VersionRejected notification, CancellationToken ct)
    {
        var organiser = await personRepository.GetByIdAsync(notification.LeadOrganiserId, ct);
        if (organiser is null) return;

        await emailService.SendEventRejectedAsync(
            organiser.Email, organiser.Name, notification.EventTitle, notification.RejectionComment, ct);
    }
}
