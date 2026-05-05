using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Content.Queries.ListPages;

public sealed class ListPagesHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : IQueryHandler<ListPagesQuery, IReadOnlyList<PageSummaryDto>>
{
    public async Task<IReadOnlyList<PageSummaryDto>> Handle(ListPagesQuery query, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan visa informationssidor.");

        var editionId = query.EditionId.HasValue ? new EditionId(query.EditionId.Value) : (EditionId?)null;

        return await pageRepository.ListAsync(convention.Id, editionId, ct);
    }
}
