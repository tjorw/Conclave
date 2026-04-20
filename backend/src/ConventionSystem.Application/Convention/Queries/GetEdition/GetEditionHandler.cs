using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.GetEdition;

public sealed class GetEditionHandler(IEditionRepository editionRepository)
    : IQueryHandler<GetEditionQuery, EditionDto?>
{
    public Task<EditionDto?> Handle(GetEditionQuery query, CancellationToken ct)
        => editionRepository.GetProjectedByIdAsync(new EditionId(query.EditionId), ct);
}
