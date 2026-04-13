using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
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

    public Task<Edition?> GetByIdWithCategoriesAndVenuesAsync(EditionId id, CancellationToken ct = default)
        => db.Editions
            .Include(e => e.Categories)
            .Include(e => e.Venues)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<IReadOnlyList<EditionSummaryDto>> ListByConventionIdAsync(ConventionId id, CancellationToken ct = default)
        => db.Editions
            .Where(e => e.ConventionId == id)
            .OrderBy(e => e.Period.StartDate)
            .Select(e => new EditionSummaryDto(
                e.Id.Value,
                e.Name,
                e.Period.StartDate,
                e.Period.EndDate,
                e.Status.ToString()))
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<EditionSummaryDto>)t.Result, TaskContinuationOptions.ExecuteSynchronously);

    public Task<EditionDto?> GetProjectedByIdAsync(EditionId id, CancellationToken ct = default)
        => db.Editions
            .Include(e => e.Venues)
            .Include(e => e.StaffAreas)
            .Include(e => e.Stations)
            .Include(e => e.Categories)
            .Where(e => e.Id == id)
            .Select(e => new EditionDto(
                e.Id.Value,
                e.ConventionId.Value,
                e.Name,
                e.Period.StartDate,
                e.Period.EndDate,
                e.Status.ToString(),
                e.OrganiserRegistrationOpen,
                e.StaffRegistrationOpen,
                e.VisitorRegistrationOpen,
                e.StaffCoordinatorId == null ? null : e.StaffCoordinatorId.Value.Value,
                e.EventCoordinatorId == null ? null : e.EventCoordinatorId.Value.Value,
                e.Venues.Select(v => new VenueDto(v.Id.Value, v.Name, v.Building, v.Description)).ToList(),
                e.StaffAreas.Select(sa => new StaffAreaDto(sa.Id.Value, sa.Name, sa.Description, sa.ResponsibleId.Value)).ToList(),
                e.Stations.Select(s => new StationDto(s.Id.Value, s.StaffAreaId.Value, s.Name, s.Description)).ToList(),
                e.Categories.Select(c => new CategoryDto(c.Id.Value, c.Name, c.Description, c.ResponsibleId.Value)).ToList()))
            .FirstOrDefaultAsync(ct);

    public void MarkAsRemoved<T>(T entity) where T : class
        => db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
