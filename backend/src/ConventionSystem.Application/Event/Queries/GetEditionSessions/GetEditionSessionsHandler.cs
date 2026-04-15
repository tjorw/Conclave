using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Event.Queries.GetEditionSessions;

public sealed class GetEditionSessionsHandler(IEventRepository eventRepository)
    : IQueryHandler<GetEditionSessionsQuery, IReadOnlyList<EditionSessionDto>>
{
    public Task<IReadOnlyList<EditionSessionDto>> Handle(GetEditionSessionsQuery query, CancellationToken ct)
        => eventRepository.ListSessionsByEditionIdAsync(new EditionId(query.EditionId), ct);
}
