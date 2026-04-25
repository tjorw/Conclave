using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.DomainEventHandlers;

public sealed class OrganizerTicketsAssignedHandler(
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IDomainEventHandler<OrganizerTicketsAssigned>
{
    public async Task Handle(OrganizerTicketsAssigned domainEvent, CancellationToken ct = default)
    {
        var personIds = domainEvent.Assignments.Select(a => a.PersonId).Distinct().ToList();
        var activeTickets = await ticketRepository.ListActiveOrganiserTicketsAsync(domainEvent.EditionId, personIds, ct);

        foreach (var assignment in domainEvent.Assignments)
        {
            var currentTickets = activeTickets
                .Where(t => t.PersonId == assignment.PersonId)
                .ToList();

            if (assignment.TicketTypeId is not null)
            {
                var ticketTypeId = assignment.TicketTypeId.Value;
                var ticketType = await ticketTypeRepository.GetByIdAsync(ticketTypeId, ct)
                    ?? throw new ResourceNotFoundException("Biljetttypen", ticketTypeId.Value.ToString());

                if (ticketType.EditionId != domainEvent.EditionId)
                    throw new DomainRuleViolationException("Arrangörsbiljetten tillhör inte arrangemangets upplaga.");

                if (ticketType.Type != TicketTypeCategory.Organiser)
                    throw new DomainRuleViolationException("Biljetttypen är inte en arrangörsbiljett.");

                if (currentTickets.Count == 1 && currentTickets[0].TicketTypeId == ticketTypeId)
                    continue;
            }

            foreach (var ticket in currentTickets)
                ticket.Revoke(domainEvent.PerformedById);

            if (assignment.TicketTypeId is not null)
            {
                var ticketTypeId = assignment.TicketTypeId.Value;
                await ticketRepository.AddAsync(Ticket.CreateOrganizerTicket(
                    ticketTypeId,
                    assignment.PersonId,
                    domainEvent.EditionId,
                    domainEvent.PerformedById), ct);
            }
        }

        await ticketRepository.SaveAsync(ct);
    }
}
