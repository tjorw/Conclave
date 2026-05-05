using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.ListPages;

public sealed record ListPagesQuery(Guid? EditionId = null) : IQuery<IReadOnlyList<PageSummaryDto>>;
