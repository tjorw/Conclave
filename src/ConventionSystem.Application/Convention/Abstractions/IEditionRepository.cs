using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IEditionRepository
{
    Task AddAndSaveAsync(Edition edition, CancellationToken ct = default);
    Task<Edition?> GetByIdAsync(EditionId id, CancellationToken ct = default);
    Task<Edition?> GetByIdWithStructureAsync(EditionId id, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
