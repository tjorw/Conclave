using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;

namespace ConventionSystem.Application.Registration.Commands.RegisterForSession;

public sealed class RegisterForSessionHandler(
    ISessionRegistrationRepository sessionRegistrationRepository,
    ITicketRepository ticketRepository,
    IRegistrationRuleService registrationRuleService,
    ICurrentUser currentUser)
    : ICommandHandler<RegisterForSessionCommand, Guid>
{
    public async Task<Guid> Handle(RegisterForSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var personId = currentUser.PersonId;
        var ticketId = new TicketId(command.TicketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new ResourceNotFoundException("Biljett", command.TicketId.ToString());

        if (ticket.Status != TicketStatus.Paid && ticket.Status != TicketStatus.Collected)
            throw new DomainRuleViolationException("Biljetten måste vara betald eller uthämtad för att registrera sig för en session.");

        if (ticket.PersonId != personId)
            throw new ForbiddenException("Biljetten tillhör inte denna person.");

        if (await sessionRegistrationRepository.HasRegistrationAsync(personId, sessionId, ct))
            throw new DomainRuleViolationException("Personen är redan registrerad för denna session.");

        if (!registrationRuleService.ValidateSeatAvailability(sessionId))
            throw new DomainRuleViolationException("Det finns inga lediga platser på denna session.");

        if (!registrationRuleService.ValidateTicket(ticketId, sessionId))
            throw new DomainRuleViolationException("Biljetten är inte giltig för denna session.");

        var registrationId = SessionRegistrationId.New();
        var registration = new SessionRegistration(registrationId, sessionId, personId, ticketId);
        await sessionRegistrationRepository.AddAndSaveAsync(registration, ct);
        return registration.Id.Value;
    }
}
