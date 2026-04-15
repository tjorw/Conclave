using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
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

    public async Task<IReadOnlyList<EditionResponsibleDto>> GetResponsiblesByEditionIdAsync(EditionId id, CancellationToken ct = default)
    {
        var edition = await db.Editions
            .Include(e => e.StaffAreas)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException("Upplagan hittades inte.");

        var events = await db.Events
            .Include(e => e.CoOrganisers)
            .Where(e => e.EditionId == id && e.Status == EventStatus.Published)
            .OrderBy(e => e.Title)
            .ToListAsync(ct);

        var personIds = new HashSet<PersonId>();
        if (edition.StaffCoordinatorId.HasValue) personIds.Add(edition.StaffCoordinatorId.Value);
        if (edition.EventCoordinatorId.HasValue) personIds.Add(edition.EventCoordinatorId.Value);
        foreach (var area in edition.StaffAreas) personIds.Add(area.ResponsibleId);
        foreach (var cat in edition.Categories) personIds.Add(cat.ResponsibleId);
        foreach (var ev in events)
        {
            personIds.Add(ev.LeadOrganiserId);
            foreach (var co in ev.CoOrganisers) personIds.Add(co.PersonId);
        }

        var personMap = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Email })
            .ToDictionaryAsync(p => p.Id, ct);

        string? GetName(PersonId pid) =>
            personMap.TryGetValue(pid, out var p) ? p.Name : null;
        string? GetEmail(PersonId pid) =>
            personMap.TryGetValue(pid, out var p) ? p.Email : null;

        var result = new List<EditionResponsibleDto>
        {
            new("Bemanningskoordinator",
                edition.StaffCoordinatorId?.Value,
                edition.StaffCoordinatorId.HasValue ? GetName(edition.StaffCoordinatorId.Value) : null,
                edition.StaffCoordinatorId.HasValue ? GetEmail(edition.StaffCoordinatorId.Value) : null),

            new("Evenemangskoordinator",
                edition.EventCoordinatorId?.Value,
                edition.EventCoordinatorId.HasValue ? GetName(edition.EventCoordinatorId.Value) : null,
                edition.EventCoordinatorId.HasValue ? GetEmail(edition.EventCoordinatorId.Value) : null),
        };

        foreach (var area in edition.StaffAreas)
            result.Add(new($"Funktionsområdesansvarig – {area.Name}",
                area.ResponsibleId.Value, GetName(area.ResponsibleId), GetEmail(area.ResponsibleId)));

        foreach (var cat in edition.Categories)
            result.Add(new($"Kategoriansvarig – {cat.Name}",
                cat.ResponsibleId.Value, GetName(cat.ResponsibleId), GetEmail(cat.ResponsibleId)));

        foreach (var ev in events)
        {
            result.Add(new($"Arrangör – {ev.Title}",
                ev.LeadOrganiserId.Value, GetName(ev.LeadOrganiserId), GetEmail(ev.LeadOrganiserId)));

            foreach (var co in ev.CoOrganisers)
                result.Add(new($"Medarrangör – {ev.Title}",
                    co.PersonId.Value, GetName(co.PersonId), GetEmail(co.PersonId)));
        }

        return result;
    }

    public void MarkAsRemoved<T>(T entity) where T : class
        => db.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
