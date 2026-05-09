using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMyTeamAssignedSessions;

public sealed class GetMyTeamAssignedSessionsHandler(
    IMyScheduleRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyTeamAssignedSessionsQuery, IReadOnlyList<MyTeamAssignedSessionDto>>
{
    public Task<IReadOnlyList<MyTeamAssignedSessionDto>> Handle(
        GetMyTeamAssignedSessionsQuery query, CancellationToken ct)
        => repository.ListMyTeamAssignedSessionsAsync(
            currentUser.PersonId, new EditionId(query.EditionId), ct);
}
