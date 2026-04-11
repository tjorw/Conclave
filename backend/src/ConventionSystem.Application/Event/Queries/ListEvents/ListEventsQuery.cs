using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Queries.ListEvents;

public sealed record ListEventsQuery(Guid EditionId) : IQuery<IReadOnlyList<EventSummaryDto>>;
