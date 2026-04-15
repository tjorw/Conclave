using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Queries.ListMyEvents;

public sealed record ListMyEventsQuery(Guid EditionId) : IQuery<IReadOnlyList<EventSummaryDto>>;
