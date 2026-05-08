using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Registration.Events;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class VisitorRegistrationConfirmedEmailHandler(
    IPersonRepository personRepository,
    IConventionRepository conventionRepository,
    IEmailService emailService)
    : IDomainEventHandler<VisitorRegistrationConfirmed>
{
    public async Task Handle(VisitorRegistrationConfirmed notification, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(notification.PersonId, ct);
        if (person is null) return;

        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null) return;

        await emailService.SendVisitorRegistrationConfirmedAsync(person.Email, person.Name, convention.Id.Value, ct);
    }
}
