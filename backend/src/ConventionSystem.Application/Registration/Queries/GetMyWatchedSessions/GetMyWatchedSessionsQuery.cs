using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMyWatchedSessions;

public sealed record GetMyWatchedSessionsQuery(Guid EditionId) : IQuery<IReadOnlyList<MyWatchedSessionSummaryDto>>;
