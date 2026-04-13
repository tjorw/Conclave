using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IConventionRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task CreateWithAdminAsync(Domain.Convention.Aggregates.Convention convention, Person admin, CancellationToken ct = default);
    Task<Domain.Convention.Aggregates.Convention?> GetByIdAsync(ConventionId id, CancellationToken ct = default);
    Task<Domain.Convention.Aggregates.Convention?> GetSingleAsync(CancellationToken ct = default);
    Task<ConventionDto?> GetProjectedByIdAsync(ConventionId id, CancellationToken ct = default);
    Task<ConventionDto?> GetProjectedAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
    /// <summary>Returnerar ActiveEditionId för konventionen (hämtas via tenant context).</summary>
    Task<EditionId?> GetActiveEditionIdAsync(CancellationToken ct = default);
}
