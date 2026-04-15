using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Queries.GetEditionSessions;

public sealed record GetEditionSessionsQuery(Guid EditionId) : IQuery<IReadOnlyList<EditionSessionDto>>;
