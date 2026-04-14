using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Event.Queries.ListEditionOrganisers;

public sealed class ListEditionOrganisersHandler(IEventRepository eventRepository)
    : IQueryHandler<ListEditionOrganisersQuery, IReadOnlyList<EditionOrganiserDto>>
{
    public Task<IReadOnlyList<EditionOrganiserDto>> Handle(ListEditionOrganisersQuery query, CancellationToken ct)
        => eventRepository.ListOrganisersByEditionIdAsync(new EditionId(query.EditionId), ct);
}
