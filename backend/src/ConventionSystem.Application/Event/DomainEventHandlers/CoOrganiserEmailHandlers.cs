using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Events;

namespace ConventionSystem.Application.Event.DomainEventHandlers;

public sealed class CoOrganiserApplicationSubmittedEmailHandler(
    IEventRepository eventRepository,
    IEmailService emailService)
    : IDomainEventHandler<CoOrganiserApplicationSubmitted>
{
    public async Task Handle(CoOrganiserApplicationSubmitted notification, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(notification.EventId, ct);
        if (ev is null) return;

        await emailService.SendCoOrganiserApplicationReceivedAsync(notification.Email, ev.Title, ct);
    }
}

public sealed class CoOrganiserApplicationApprovedEmailHandler(
    IEventRepository eventRepository,
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<CoOrganiserApplicationApproved>
{
    public async Task Handle(CoOrganiserApplicationApproved notification, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(notification.EventId, ct);
        if (ev is null) return;

        var person = await personRepository.GetByIdAsync(notification.PersonId, ct);
        if (person is null) return;

        await emailService.SendCoOrganiserApplicationApprovedAsync(person.Email, person.Name, ev.Title, ct);
    }
}

public sealed class CoOrganiserApplicationRejectedEmailHandler(
    IEventRepository eventRepository,
    IEmailService emailService)
    : IDomainEventHandler<CoOrganiserApplicationRejected>
{
    public async Task Handle(CoOrganiserApplicationRejected notification, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithCoOrganisersAndApplicationsAsync(notification.EventId, ct);
        if (ev is null) return;

        var application = ev.CoOrganiserApplications.FirstOrDefault(a => a.Id == notification.ApplicationId);
        if (application is null) return;

        await emailService.SendCoOrganiserApplicationRejectedAsync(
            application.Email,
            application.Name ?? application.Email,
            ev.Title,
            notification.Comment,
            ct);
    }
}
