using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;

public sealed class ListEditionResponsiblesHandler(IEditionRepository editionRepository)
    : IQueryHandler<ListEditionResponsiblesQuery, IReadOnlyList<EditionResponsibleDto>>
{
    public Task<IReadOnlyList<EditionResponsibleDto>> Handle(ListEditionResponsiblesQuery query, CancellationToken ct)
        => editionRepository.GetResponsiblesByEditionIdAsync(new EditionId(query.EditionId), ct);
}
