using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Event.Queries.GetEditionSessions;

public sealed class GetEditionSessionsHandler(
    IEventRepository eventRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetEditionSessionsQuery, IReadOnlyList<EditionSessionDto>>
{
    public Task<IReadOnlyList<EditionSessionDto>> Handle(GetEditionSessionsQuery query, CancellationToken ct)
    {
        if (!currentUser.IsAdmin && !currentUser.IsReception)
            throw new ForbiddenException("Utföraren har inte behörighet att visa sessioner.");

        return eventRepository.ListSessionsByEditionIdAsync(new EditionId(query.EditionId), ct);
    }
}
