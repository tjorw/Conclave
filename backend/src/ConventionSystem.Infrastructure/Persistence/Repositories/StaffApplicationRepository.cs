using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class StaffApplicationRepository(ConventionDbContext db) : IStaffApplicationRepository
{
    public Task<StaffApplication?> GetByIdAsync(StaffApplicationId id, CancellationToken ct = default)
        => db.StaffApplications.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<StaffApplication?> GetByIdWithDetailsAsync(StaffApplicationId id, CancellationToken ct = default)
        => db.StaffApplications
            .Include(a => a.Availabilities)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> HasActiveApplicationAsync(PersonId personId, EditionId editionId, CancellationToken ct = default)
        => db.StaffApplications.AnyAsync(
            a => a.PersonId == personId && a.EditionId == editionId
              && a.Status != StaffApplicationStatus.Rejected, ct);

    public async Task<MyStaffApplicationDto?> GetByPersonAndEditionAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var application = await db.StaffApplications
            .FirstOrDefaultAsync(a => a.PersonId == personId && a.EditionId == editionId
                                      && a.Status != StaffApplicationStatus.Rejected, ct);

        return application is null
            ? null
            : new MyStaffApplicationDto(application.Id.Value, application.Status.ToString());
    }

    public async Task<IReadOnlyList<StaffApplicationSummaryDto>> ListByEditionIdAsync(EditionId editionId, CancellationToken ct = default)
    {
        var applications = await db.StaffApplications
            .Include(a => a.StationPreferences)
            .Include(a => a.Availabilities)
            .Where(a => a.EditionId == editionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var personIds = applications.Select(a => a.PersonId).Distinct().ToHashSet();
        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return applications.Select(a => new StaffApplicationSummaryDto(
            a.Id.Value,
            a.PersonId.Value,
            personNames.GetValueOrDefault(a.PersonId),
            a.InterestDescription,
            a.Status.ToString(),
            a.CreatedAt,
            a.StationPreferences.Select(p => p.StationId.Value).ToList(),
            a.Availabilities.OrderBy(av => av.TimeSlot.Start)
                            .Select(av => new StaffApplicationAvailabilityDto(av.TimeSlot.Start, av.TimeSlot.End))
                            .ToList()
        )).ToList();
    }

    public async Task<IReadOnlyList<EditionStaffMemberDto>> ListApprovedByEditionIdAsync(EditionId editionId, CancellationToken ct = default)
    {
        var applications = await db.StaffApplications
            .Where(a => a.EditionId == editionId &&
                       (a.Status == StaffApplicationStatus.Assigned || a.Status == StaffApplicationStatus.Confirmed))
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var personIds = applications.Select(a => a.PersonId).Distinct().ToHashSet();
        var personMap = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone })
            .ToDictionaryAsync(p => p.Id, ct);

        return applications.Select(a =>
        {
            personMap.TryGetValue(a.PersonId, out var p);
            return new EditionStaffMemberDto(a.PersonId.Value, p?.Name ?? "", p?.Email ?? "", p?.Phone, a.Status.ToString());
        }).ToList();
    }

    public async Task AddAndSaveAsync(StaffApplication application, CancellationToken ct = default)
    {
        db.StaffApplications.Add(application);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
