using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;
using MediatR;

namespace ConventionSystem.Application.Registration.Commands.RegisterForSession;

public sealed class RegisterForSessionHandler(
    ISessionRegistrationRepository sessionRegistrationRepository,
    ITicketRepository ticketRepository,
    IRegistrationRuleService registrationRuleService)
    : IRequestHandler<RegisterForSessionCommand, Guid>
{
    public async Task<Guid> Handle(RegisterForSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var personId = new PersonId(command.PersonId);
        var ticketId = new TicketId(command.TicketId);

        var ticket = await ticketRepository.GetByIdAsync(ticketId, ct)
            ?? throw new InvalidOperationException($"Biljetten '{command.TicketId}' hittades inte.");

        if (ticket.Status != TicketStatus.Paid && ticket.Status != TicketStatus.Collected)
            throw new InvalidOperationException("Biljetten måste vara betald eller uthämtad för att registrera sig för en session.");

        if (ticket.PersonId != personId)
            throw new InvalidOperationException("Biljetten tillhör inte denna person.");

        if (await sessionRegistrationRepository.HasRegistrationAsync(personId, sessionId, ct))
            throw new InvalidOperationException("Personen är redan registrerad för denna session.");

        if (!registrationRuleService.ValidateSeatAvailability(sessionId))
            throw new InvalidOperationException("Det finns inga lediga platser på denna session.");

        if (!registrationRuleService.ValidateTicket(ticketId, sessionId))
            throw new InvalidOperationException("Biljetten är inte giltig för denna session.");

        var registrationId = SessionRegistrationId.New();
        var registration = new SessionRegistration(registrationId, sessionId, personId, ticketId);
        await sessionRegistrationRepository.AddAndSaveAsync(registration, ct);
        return registration.Id.Value;
    }
}
