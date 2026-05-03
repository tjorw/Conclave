using ConventionSystem.Application.Content;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
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

    public async Task<IReadOnlyList<PageSummaryDto>> ListAsync(ConventionId conventionId, CancellationToken ct = default)
        => await db.Pages
            .Where(p => p.ConventionId == conventionId)
            .OrderBy(p => p.Title)
            .Select(p => new PageSummaryDto(
                p.Id.Value,
                p.Slug,
                p.Title,
                p.EditionId == null ? null : p.EditionId.Value.Value,
                p.IsPublished,
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
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);

    public async Task<PublicPageDto?> GetPublishedBySlugAsync(
        ConventionId conventionId,
        EditionId? activeEditionId,
        string slug,
        CancellationToken ct = default)
    {
        if (activeEditionId is not null)
        {
            var editionPage = await db.Pages
                .Where(p => p.ConventionId == conventionId
                    && p.EditionId == activeEditionId
                    && p.Slug == slug
                    && p.IsPublished)
                .Select(p => new PublicPageDto(
                    p.Slug,
                    p.Title,
                    p.Content,
                    p.EditionId == null ? null : p.EditionId.Value.Value))
                .FirstOrDefaultAsync(ct);

            if (editionPage is not null)
                return editionPage;
        }

        return await db.Pages
            .Where(p => p.ConventionId == conventionId
                && p.EditionId == null
                && p.Slug == slug
                && p.IsPublished)
            .Select(p => new PublicPageDto(
                p.Slug,
                p.Title,
                p.Content,
                p.EditionId == null ? null : p.EditionId.Value.Value))
            .FirstOrDefaultAsync(ct);
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
