using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Convention.Queries.ListEditions;

public sealed record ListEditionsQuery(Guid ConventionId) : IQuery<IReadOnlyList<EditionSummaryDto>>;
