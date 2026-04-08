using ConventionSystem.Domain.Convention.Aggregates;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IConventionRepository
{
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Domain.Convention.Aggregates.Convention convention, CancellationToken ct = default);
}
