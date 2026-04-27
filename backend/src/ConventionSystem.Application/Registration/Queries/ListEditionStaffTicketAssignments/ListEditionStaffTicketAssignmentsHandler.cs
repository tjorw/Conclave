using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListEditionStaffTicketAssignments;

public sealed class ListEditionStaffTicketAssignmentsHandler(
    IStaffApplicationRepository staffApplicationRepository,
    ITicketRepository ticketRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IQueryHandler<ListEditionStaffTicketAssignmentsQuery, IReadOnlyList<StaffTicketAssignmentDto>>
{
    public async Task<IReadOnlyList<StaffTicketAssignmentDto>> Handle(
        ListEditionStaffTicketAssignmentsQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var staffMembers = await staffApplicationRepository.ListApprovedByEditionIdAsync(editionId, ct);
        var staffIds = staffMembers.Select(s => new PersonId(s.PersonId)).Distinct().ToList();
        var activeTickets = await ticketRepository.ListActiveStaffTicketsAsync(editionId, staffIds, ct);
        var ticketTypes = await ticketTypeRepository.ListByEditionIdAsync(editionId, ct);
        var ticketTypeNames = ticketTypes.ToDictionary(t => t.Id, t => t.Name);

        return staffIds
            .Select(personId =>
            {
                var ticket = activeTickets
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefault(t => t.PersonId == personId);

                return new StaffTicketAssignmentDto(
                    personId.Value,
                    ticket?.Id.Value,
                    ticket?.TicketTypeId.Value,
                    ticket is null ? null : ticketTypeNames.GetValueOrDefault(ticket.TicketTypeId.Value),
                    ticket?.Status.ToString());
            })
            .ToList();
    }
}
