using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IConventionRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task CreateWithAdminAsync(Domain.Convention.Aggregates.Convention convention, Person admin, CancellationToken ct = default);
    Task<Domain.Convention.Aggregates.Convention?> GetByIdAsync(ConventionId id, CancellationToken ct = default);
}
