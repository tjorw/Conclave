using ConventionSystem.Application.Common;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;

namespace ConventionSystem.Application.Content.Queries.ListPublicMenuPages;

public sealed class ListPublicMenuPagesHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository) : IQueryHandler<ListPublicMenuPagesQuery, IReadOnlyList<PublicPageMenuItemDto>>
{
    public async Task<IReadOnlyList<PublicPageMenuItemDto>> Handle(ListPublicMenuPagesQuery query, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct);
        if (convention is null) return [];

        return await pageRepository.ListPublicMenuPagesAsync(convention.Id, convention.ActiveEditionId, ct);
    }
}
