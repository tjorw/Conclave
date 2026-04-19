using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMyOrganiserSessions;

public sealed record GetMyOrganiserSessionsQuery(Guid EditionId) : IQuery<IReadOnlyList<MyOrganiserSessionSummaryDto>>;
