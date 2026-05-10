using ConventionSystem.Application.Content;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Entities;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class PageRepository(ConventionDbContext db) : IPageRepository
{
    public async Task AddAsync(Page page, CancellationToken ct = default)
        => await db.Pages.AddAsync(page, ct);

    public Task<Page?> GetByIdAsync(PageId id, CancellationToken ct = default)
        => db.Pages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Page?> GetByIdWithTranslationsAsync(PageId id, CancellationToken ct = default)
        => db.Pages.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PageTranslation?> GetTranslationAsync(PageId id, string locale, CancellationToken ct = default)
        => db.Set<PageTranslation>()
            .FirstOrDefaultAsync(t => EF.Property<PageId>(t, "PageId") == id && t.Locale == locale.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<PageSummaryDto>> ListAsync(ConventionId conventionId, EditionId? editionId, CancellationToken ct = default)
        => await db.Pages
            .Where(p => p.ConventionId == conventionId)
            .Where(p => p.EditionId == editionId)
            .OrderBy(p => p.Title)
            .Select(p => new PageSummaryDto(
                p.Id.Value,
                p.Slug,
                p.Title,
                p.EditionId == null ? null : p.EditionId.Value.Value,
                p.IsPublished,
                p.ShowInPublicMenu,
                p.MenuSortOrder,
                p.UpdatedAt))
            .ToListAsync(ct);

    public Task<PageDto?> GetProjectedByIdAsync(PageId id, CancellationToken ct = default)
        => db.Pages
            .Where(p => p.Id == id)
            .Select(p => new PageDto(
                p.Id.Value,
                p.Slug,
                p.Title,
                p.Content,
                p.EditionId == null ? null : p.EditionId.Value.Value,
                p.IsPublished,
                p.ShowInPublicMenu,
                p.MenuSortOrder,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<PublicPageDto?> GetPublishedBySlugAsync(
        ConventionId conventionId,
        EditionId? activeEditionId,
        string slug,
        string? locale,
        CancellationToken ct = default)
    {
        Page? page = null;

        if (activeEditionId is not null)
        {
            page = await db.Pages
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.ConventionId == conventionId
                    && p.EditionId == activeEditionId
                    && p.Slug == slug
                    && p.IsPublished, ct);
        }

        page ??= await db.Pages
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.ConventionId == conventionId
                && p.EditionId == null
                && p.Slug == slug
                && p.IsPublished, ct);

        if (page is null) return null;

        var translation = locale is not null
            ? page.Translations.FirstOrDefault(t => t.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase))
            : null;

        return new PublicPageDto(
            page.Slug,
            translation?.Title ?? page.Title,
            translation?.Content ?? page.Content,
            page.EditionId?.Value);
    }

    public async Task<IReadOnlyList<PublicPageMenuItemDto>> ListPublicMenuPagesAsync(
        ConventionId conventionId,
        EditionId? activeEditionId,
        string? locale,
        CancellationToken ct = default)
    {
        var pages = await db.Pages
            .Include(p => p.Translations)
            .Where(p => p.ConventionId == conventionId
                && p.IsPublished
                && p.ShowInPublicMenu
                && (p.EditionId == null || (activeEditionId != null && p.EditionId == activeEditionId)))
            .ToListAsync(ct);

        return pages
            .GroupBy(p => p.Slug)
            .Select(group => group
                .OrderByDescending(p => activeEditionId != null && p.EditionId == activeEditionId)
                .ThenBy(p => p.EditionId.HasValue)
                .First())
            .OrderBy(p => p.MenuSortOrder)
            .ThenBy(p => p.Title)
            .Select(p =>
            {
                var translation = locale is not null
                    ? p.Translations.FirstOrDefault(t => t.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase))
                    : null;

                return new PublicPageMenuItemDto(
                    p.Slug,
                    translation?.Title ?? p.Title,
                    p.MenuSortOrder,
                    p.EditionId?.Value);
            })
            .ToList();
    }

    public Task<bool> SlugExistsAsync(
        ConventionId conventionId,
        EditionId? editionId,
        string slug,
        PageId? excludingPageId = null,
        CancellationToken ct = default)
        => db.Pages.AnyAsync(p =>
            p.ConventionId == conventionId
            && p.EditionId == editionId
            && p.Slug == slug
            && (excludingPageId == null || p.Id != excludingPageId.Value), ct);

    public void Remove(Page page)
        => db.Pages.Remove(page);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
