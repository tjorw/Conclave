using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListAvailableTicketTypes;

public sealed class ListAvailableTicketTypesHandler(
    ITicketTypeRepository ticketTypeRepository,
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListAvailableTicketTypesQuery, IReadOnlyList<VisitorTicketTypeDto>>
{
    public async Task<IReadOnlyList<VisitorTicketTypeDto>> Handle(ListAvailableTicketTypesQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var all = await ticketTypeRepository.ListByEditionIdAsync(editionId, ct);

        var visibleAndSellableVisitorTypes = all
            .Where(t => t.Category == "Visitor")
            .ToList();

        if (visibleAndSellableVisitorTypes.Count == 0)
            return [];

        var available = new List<VisitorTicketTypeDto>(visibleAndSellableVisitorTypes.Count);

        foreach (var ticketType in visibleAndSellableVisitorTypes)
        {
            var hasActiveRegistration = await visitorRegistrationRepository.HasActiveRegistrationForTicketTypeAsync(
                currentUser.PersonId,
                editionId,
                new TicketTypeId(ticketType.Id),
                ct);

            if (!hasActiveRegistration)
            {
                available.Add(new VisitorTicketTypeDto(
                    ticketType.Id,
                    ticketType.Name,
                    ticketType.Price,
                    ticketType.Description));
            }
        }

        return available;
    }
}
