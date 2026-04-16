using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyWatchedSessions;

public sealed class GetMyWatchedSessionsHandler(
    ISessionWatchRepository sessionWatchRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyWatchedSessionsQuery, IReadOnlyList<MyWatchedSessionSummaryDto>>
{
    public Task<IReadOnlyList<MyWatchedSessionSummaryDto>> Handle(GetMyWatchedSessionsQuery query, CancellationToken ct)
        => sessionWatchRepository.ListByPersonAndEditionAsync(
            currentUser.PersonId,
            new EditionId(query.EditionId),
            ct);
}
