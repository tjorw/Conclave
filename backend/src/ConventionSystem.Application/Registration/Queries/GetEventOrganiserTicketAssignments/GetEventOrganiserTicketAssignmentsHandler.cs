using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetEventOrganiserTicketAssignments;

public sealed class GetEventOrganiserTicketAssignmentsHandler(
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IQueryHandler<GetEventOrganiserTicketAssignmentsQuery, IReadOnlyList<OrganiserTicketAssignmentDto>>
{
    public async Task<IReadOnlyList<OrganiserTicketAssignmentDto>> Handle(GetEventOrganiserTicketAssignmentsQuery query, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(query.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", query.EventId.ToString());

        var organiserIds = ev.CoOrganisers.Select(c => c.PersonId).Append(ev.LeadOrganiserId).Distinct().ToList();
        var activeTickets = await ticketRepository.ListActiveOrganiserTicketsAsync(ev.EditionId, organiserIds, ct);
        var ticketTypes = await ticketTypeRepository.ListByEditionIdAsync(ev.EditionId, ct);
        var ticketTypeNames = ticketTypes.ToDictionary(t => t.Id, t => t.Name);

        return organiserIds
            .Select(personId =>
            {
                var ticket = activeTickets
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault(t => t.PersonId == personId);

                return new OrganiserTicketAssignmentDto(
                    personId.Value,
                    ticket?.Id.Value,
                    ticket?.TicketTypeId.Value,
                    ticket is null ? null : ticketTypeNames.GetValueOrDefault(ticket.TicketTypeId.Value),
                    ticket?.Status.ToString());
            })
            .ToList();
    }
}
