using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Queries.ListTeamAssignmentsForSession;

public sealed class ListTeamAssignmentsForSessionHandler(
    ITeamSessionAssignmentRepository repository)
    : IQueryHandler<ListTeamAssignmentsForSessionQuery, IReadOnlyList<TeamSessionAssignmentDto>>
{
    public Task<IReadOnlyList<TeamSessionAssignmentDto>> Handle(
        ListTeamAssignmentsForSessionQuery query, CancellationToken ct)
        => repository.ListBySessionIdAsync(new SessionId(query.SessionId), ct);
}
