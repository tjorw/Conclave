using ConventionSystem.Domain.Convention.Aggregates;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IEditionRepository
{
    Task AddAndSaveAsync(Edition edition, CancellationToken ct = default);
}
