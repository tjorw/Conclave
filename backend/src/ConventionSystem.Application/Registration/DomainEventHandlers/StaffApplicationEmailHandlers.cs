using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Registration.Events;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class StaffApplicationReceivedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<StaffApplicationReceived>
{
    public async Task Handle(StaffApplicationReceived notification, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(notification.PersonId, ct);
        if (person is null) return;

        await emailService.SendStaffApplicationReceivedAsync(person.Email, person.Name, ct);
    }
}

public sealed class StaffApplicationAcceptedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<StaffApplicationAccepted>
{
    public async Task Handle(StaffApplicationAccepted notification, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(notification.PersonId, ct);
        if (person is null) return;

        await emailService.SendStaffApplicationAcceptedAsync(person.Email, person.Name, ct);
    }
}

public sealed class StaffApplicationRejectedEmailHandler(
    IPersonRepository personRepository,
    IEmailService emailService)
    : IDomainEventHandler<StaffApplicationRejected>
{
    public async Task Handle(StaffApplicationRejected notification, CancellationToken ct)
    {
        var person = await personRepository.GetByIdAsync(notification.PersonId, ct);
        if (person is null) return;

        await emailService.SendStaffApplicationRejectedAsync(person.Email, person.Name, ct);
    }
}
