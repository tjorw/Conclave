using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.ListEditionResponsibles;

public sealed class ListEditionResponsiblesHandler(
    IEditionRepository editionRepository,
    ICurrentUser currentUser)
    : IQueryHandler<ListEditionResponsiblesQuery, IReadOnlyList<EditionResponsibleDto>>
{
    public Task<IReadOnlyList<EditionResponsibleDto>> Handle(ListEditionResponsiblesQuery query, CancellationToken ct)
    {
        if (!currentUser.IsAdmin && !currentUser.IsReception)
            throw new ForbiddenException("Utföraren har inte behörighet att visa ansvariga.");

        return editionRepository.GetResponsiblesByEditionIdAsync(new EditionId(query.EditionId), ct);
    }
}
