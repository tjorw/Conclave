using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Queries.GetPage;

public sealed class GetPageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : IQueryHandler<GetPageQuery, PageDto?>
{
    public async Task<PageDto?> Handle(GetPageQuery query, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan visa informationssidor.");

        return await pageRepository.GetProjectedByIdAsync(new PageId(query.PageId), ct);
    }
}
