using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Queries;

namespace ConventionSystem.Application.Event.Queries.ListTeamAssignmentsForSession;

public sealed record ListTeamAssignmentsForSessionQuery(Guid EventId, Guid SessionId)
    : IQuery<IReadOnlyList<TeamSessionAssignmentDto>>;
