using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Content.Abstractions;

public interface IPageRepository
{
    Task AddAsync(Page page, CancellationToken ct = default);
    Task<Page?> GetByIdAsync(PageId id, CancellationToken ct = default);
    Task<IReadOnlyList<PageSummaryDto>> ListAsync(ConventionId conventionId, CancellationToken ct = default);
    Task<PageDto?> GetProjectedByIdAsync(PageId id, CancellationToken ct = default);
    Task<PublicPageDto?> GetPublishedBySlugAsync(ConventionId conventionId, EditionId? activeEditionId, string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(ConventionId conventionId, EditionId? editionId, string slug, PageId? excludingPageId = null, CancellationToken ct = default);
    void Remove(Page page);
    Task SaveAsync(CancellationToken ct = default);
}
