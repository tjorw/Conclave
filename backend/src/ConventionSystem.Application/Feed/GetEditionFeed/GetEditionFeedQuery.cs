using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Feed.GetEditionFeed;

public record GetEditionFeedQuery(Guid EditionId, string? Locale = null) : IQuery<EditionFeedDto?>;
