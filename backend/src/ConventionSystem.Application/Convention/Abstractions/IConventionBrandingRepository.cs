using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IConventionBrandingRepository
{
    Task<ConventionBranding?> GetByConventionIdAsync(ConventionId conventionId, CancellationToken ct = default);
    Task<ConventionBrandingDto?> GetProjectedByConventionIdAsync(ConventionId conventionId, CancellationToken ct = default);
    Task AddAsync(ConventionBranding branding, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
