using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyWatchedSessions;

public sealed record GetMyWatchedSessionsQuery(Guid EditionId) : IRequest<IReadOnlyList<MyWatchedSessionSummaryDto>>;
