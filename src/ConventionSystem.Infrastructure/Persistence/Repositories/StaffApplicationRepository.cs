using ConventionSystem.Application.Registration.Abstractions;
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

    public async Task AddAndSaveAsync(StaffApplication application, CancellationToken ct = default)
    {
        db.StaffApplications.Add(application);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
