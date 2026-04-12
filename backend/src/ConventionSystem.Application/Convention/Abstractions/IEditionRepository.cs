using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IEditionRepository
{
    Task AddAndSaveAsync(Edition edition, CancellationToken ct = default);
    Task<Edition?> GetByIdAsync(EditionId id, CancellationToken ct = default);
    Task<Edition?> GetByIdWithStructureAsync(EditionId id, CancellationToken ct = default);
    Task<Edition?> GetByIdWithStaffAreasAsync(EditionId id, CancellationToken ct = default);
    Task<Edition?> GetByStationIdAsync(StationId stationId, CancellationToken ct = default);
    Task<Edition?> GetByIdWithCategoriesAsync(EditionId id, CancellationToken ct = default);
    Task<Edition?> GetByIdWithCategoriesAndVenuesAsync(EditionId id, CancellationToken ct = default);
    Task<IReadOnlyList<EditionSummaryDto>> ListByConventionIdAsync(ConventionId id, CancellationToken ct = default);
    Task<EditionDto?> GetProjectedByIdAsync(EditionId id, CancellationToken ct = default);
    void MarkAsAdded<T>(T entity) where T : class;
    void MarkAsRemoved<T>(T entity) where T : class;
    Task SaveAsync(CancellationToken ct = default);
}
