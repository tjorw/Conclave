using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;

namespace ConventionSystem.Application.Registration.Queries.ListOrganiserTicketTypes;

public sealed class ListOrganiserTicketTypesHandler(ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<ListOrganiserTicketTypesQuery, IReadOnlyList<OrganiserTicketTypeDto>>
{
    public async Task<IReadOnlyList<OrganiserTicketTypeDto>> Handle(ListOrganiserTicketTypesQuery query, CancellationToken ct)
    {
        var all = await ticketTypeRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);

        return all
            .Where(t => t.Category == nameof(TicketTypeCategory.Organiser))
            .Select(t => new OrganiserTicketTypeDto(t.Id, t.Name, t.Price, t.Description))
            .ToList();
    }
}
