using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Queries.GetPageTranslation;

public sealed class GetPageTranslationHandler(IPageRepository pageRepository)
    : IQueryHandler<GetPageTranslationQuery, PageTranslationDto?>
{
    public async Task<PageTranslationDto?> Handle(GetPageTranslationQuery query, CancellationToken ct)
    {
        var pageId = new PageId(query.PageId);
        var page = await pageRepository.GetByIdAsync(pageId, ct)
            ?? throw new ResourceNotFoundException("Sida", query.PageId.ToString());

        var translation = await pageRepository.GetTranslationAsync(pageId, query.Locale, ct);
        if (translation is null) return null;

        return new PageTranslationDto(query.PageId, translation.Locale, translation.Title, translation.Content);
    }
}
