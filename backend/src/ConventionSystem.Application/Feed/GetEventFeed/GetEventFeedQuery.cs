using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Feed.GetEventFeed;

public record GetEventFeedQuery(Guid EventId, string? Locale = null) : IQuery<EventFeedDto?>;
