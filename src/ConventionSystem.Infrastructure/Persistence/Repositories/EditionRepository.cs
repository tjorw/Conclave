using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class EditionRepository(ConventionDbContext db) : IEditionRepository
{
    public async Task AddAndSaveAsync(Edition edition, CancellationToken ct = default)
    {
        await db.Editions.AddAsync(edition, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task<Edition?> GetByIdAsync(EditionId id, CancellationToken ct = default)
        => db.Editions.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Edition?> GetByIdWithStructureAsync(EditionId id, CancellationToken ct = default)
        => db.Editions
            .Include(e => e.Venues)
            .Include(e => e.StaffAreas)
            .Include(e => e.Stations)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Edition?> GetByIdWithStaffAreasAsync(EditionId id, CancellationToken ct = default)
        => db.Editions
            .Include(e => e.StaffAreas)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Edition?> GetByStationIdAsync(StationId stationId, CancellationToken ct = default)
        => db.Editions
            .Include(e => e.StaffAreas)
            .Include(e => e.Stations)
            .FirstOrDefaultAsync(e => e.Stations.Any(s => s.Id == stationId), ct);

    public Task<Edition?> GetByIdWithCategoriesAsync(EditionId id, CancellationToken ct = default)
        => db.Editions
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
