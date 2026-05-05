using ConventionSystem.Application.Common;
using ConventionSystem.Application.Feed;

namespace ConventionSystem.Application.Event.Queries.GetFeaturedEvents;

public sealed record GetFeaturedEventsQuery() : IQuery<IReadOnlyList<EventSummaryFeedDto>>;
