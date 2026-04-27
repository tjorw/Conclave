using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListEditionOrganiserTicketAssignments;

public sealed class ListEditionOrganiserTicketAssignmentsHandler(
    IEventRepository eventRepository,
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IQueryHandler<ListEditionOrganiserTicketAssignmentsQuery, IReadOnlyList<OrganiserTicketAssignmentDto>>
{
    public async Task<IReadOnlyList<OrganiserTicketAssignmentDto>> Handle(ListEditionOrganiserTicketAssignmentsQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var organisers = await eventRepository.ListOrganisersByEditionIdAsync(editionId, ct);
        var organiserIds = organisers.Select(o => new PersonId(o.PersonId)).Distinct().ToList();
        var activeTickets = await ticketRepository.ListActiveOrganiserTicketsAsync(editionId, organiserIds, ct);
        var ticketTypes = await ticketTypeRepository.ListByEditionIdAsync(editionId, ct);
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
