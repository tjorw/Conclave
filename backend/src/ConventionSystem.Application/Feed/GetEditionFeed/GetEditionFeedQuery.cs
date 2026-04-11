using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Feed.GetEditionFeed;

public record GetEditionFeedQuery(Guid EditionId) : IQuery<EditionFeedDto?>;
