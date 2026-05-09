using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;

namespace ConventionSystem.Application.Registration.Commands.RegisterForSession;

public sealed class RegisterForSessionHandler(
    ISessionRegistrationRepository sessionRegistrationRepository,
    ITicketRepository ticketRepository,
    IEventRepository eventRepository,
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

        if (!registrationRuleService.ValidateTicket(ticketId, sessionId))
            throw new DomainRuleViolationException("Biljetten är inte giltig för denna session.");

        var allocationInfo = await eventRepository.GetSessionAllocationInfoAsync(sessionId, ct)
            ?? throw new ResourceNotFoundException("Session", command.SessionId.ToString());

        var status = SessionRegistrationStatus.Confirmed;

        if (allocationInfo.AllocationMode == AllocationMode.Queue)
        {
            var confirmed = await sessionRegistrationRepository.CountConfirmedBySessionIdAsync(sessionId, ct);
            if (confirmed >= allocationInfo.MaxSeats)
                status = SessionRegistrationStatus.Pending;
        }
        else
        {
            var confirmed = await sessionRegistrationRepository.CountConfirmedBySessionIdAsync(sessionId, ct);
            if (confirmed >= allocationInfo.MaxSeats)
                throw new DomainRuleViolationException("Det finns inga lediga platser på denna session.");
        }

        var registrationId = SessionRegistrationId.New();
        var registration = new SessionRegistration(registrationId, sessionId, personId, ticketId, status);
        await sessionRegistrationRepository.AddAndSaveAsync(registration, ct);
        return registration.Id.Value;
    }
}
