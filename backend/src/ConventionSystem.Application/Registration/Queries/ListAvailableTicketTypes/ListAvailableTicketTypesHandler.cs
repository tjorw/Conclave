using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;

public sealed class ListAvailableTicketTypesHandler(ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<ListAvailableTicketTypesQuery, IReadOnlyList<VisitorTicketTypeDto>>
{
    public async Task<IReadOnlyList<VisitorTicketTypeDto>> Handle(ListAvailableTicketTypesQuery query, CancellationToken ct)
    {
        var all = await ticketTypeRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);

        return all
            .Where(t => t.Category == "Visitor" && t.IsSellable && t.IsPubliclyVisible)
            .Select(t => new VisitorTicketTypeDto(t.Id, t.Name, t.Price))
            .ToList();
    }
}
