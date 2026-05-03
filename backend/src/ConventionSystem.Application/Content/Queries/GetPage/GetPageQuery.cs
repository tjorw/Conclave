using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.GetPage;

public sealed record GetPageQuery(Guid PageId) : IQuery<PageDto?>;
