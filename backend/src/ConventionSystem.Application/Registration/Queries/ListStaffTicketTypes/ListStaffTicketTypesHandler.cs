using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;

namespace ConventionSystem.Application.Registration.Queries.ListStaffTicketTypes;

public sealed class ListStaffTicketTypesHandler(ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<ListStaffTicketTypesQuery, IReadOnlyList<StaffTicketTypeDto>>
{
    public async Task<IReadOnlyList<StaffTicketTypeDto>> Handle(ListStaffTicketTypesQuery query, CancellationToken ct)
    {
        var all = await ticketTypeRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);

        return all
            .Where(t => t.Category == nameof(TicketTypeCategory.Staff))
            .Select(t => new StaffTicketTypeDto(t.Id, t.Name, t.Price, t.Description))
            .ToList();
    }
}
