using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListTicketTypes;

public sealed class ListTicketTypesHandler(ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<ListTicketTypesQuery, IReadOnlyList<TicketTypeAdminDto>>
{
    public Task<IReadOnlyList<TicketTypeAdminDto>> Handle(ListTicketTypesQuery query, CancellationToken ct)
        => ticketTypeRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);
}
