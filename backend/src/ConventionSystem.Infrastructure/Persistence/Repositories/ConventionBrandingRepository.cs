using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class ConventionBrandingRepository(ConventionDbContext db) : IConventionBrandingRepository
{
    public Task<ConventionBranding?> GetByConventionIdAsync(ConventionId conventionId, CancellationToken ct = default)
        => db.ConventionBrandings.FirstOrDefaultAsync(b => b.ConventionId == conventionId, ct);

    public Task<ConventionBrandingDto?> GetProjectedByConventionIdAsync(ConventionId conventionId, CancellationToken ct = default)
        => db.ConventionBrandings
            .Where(b => b.ConventionId == conventionId)
            .Select(b => new ConventionBrandingDto(
                b.ConventionId.Value,
                b.PrimaryColor,
                b.AccentColor,
                b.LogoUrl,
                b.FaviconUrl,
                b.FontFamily,
                b.CustomCss))
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(ConventionBranding branding, CancellationToken ct = default)
        => db.ConventionBrandings.AddAsync(branding, ct).AsTask();

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
