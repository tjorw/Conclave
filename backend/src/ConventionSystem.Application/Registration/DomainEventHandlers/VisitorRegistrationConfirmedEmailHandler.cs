using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Registration.Events;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class VisitorRegistrationConfirmedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<VisitorRegistrationConfirmed>
{
    public async Task Handle(VisitorRegistrationConfirmed notification, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(notification.PersonId, ct);
        if (person is null) return;

        await emailService.SendVisitorRegistrationConfirmedAsync(person.Email, person.Name, ct);
    }
}
