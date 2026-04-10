using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.ListEditions;

public sealed class ListEditionsHandler(IEditionRepository editionRepository)
    : IQueryHandler<ListEditionsQuery, IReadOnlyList<EditionSummaryDto>>
{
    public Task<IReadOnlyList<EditionSummaryDto>> Handle(ListEditionsQuery query, CancellationToken ct)
        => editionRepository.ListByConventionIdAsync(new ConventionId(query.ConventionId), ct);
}
