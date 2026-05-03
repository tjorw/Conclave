using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;

namespace ConventionSystem.Application.Content.Queries.GetPublicPage;

public sealed class GetPublicPageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository) : IQueryHandler<GetPublicPageQuery, PublicPageDto?>
{
    public async Task<PublicPageDto?> Handle(GetPublicPageQuery query, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null) return null;

        return await pageRepository.GetPublishedBySlugAsync(
            convention.Id,
            convention.ActiveEditionId,
            query.Slug.Trim().ToLowerInvariant(),
            ct);
    }
}
