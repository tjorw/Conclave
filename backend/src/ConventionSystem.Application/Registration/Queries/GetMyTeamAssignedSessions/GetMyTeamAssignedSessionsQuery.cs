using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMyTeamAssignedSessions;

public sealed record GetMyTeamAssignedSessionsQuery(Guid EditionId)
    : IQuery<IReadOnlyList<MyTeamAssignedSessionDto>>;
