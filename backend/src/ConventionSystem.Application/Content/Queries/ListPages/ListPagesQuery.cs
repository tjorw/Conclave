using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Content.Queries.ListPages;

public sealed record ListPagesQuery : IQuery<IReadOnlyList<PageSummaryDto>>;
