using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class VisitorRegistrationRepository(ConventionDbContext db) : IVisitorRegistrationRepository
{
    public Task<VisitorRegistration?> GetByIdAsync(VisitorRegistrationId id, CancellationToken ct = default)
        => db.VisitorRegistrations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> HasActiveRegistrationAsync(PersonId personId, EditionId editionId, CancellationToken ct = default)
        => db.VisitorRegistrations.AnyAsync(
            r => r.PersonId == personId && r.EditionId == editionId && r.Status != VisitorRegistrationStatus.Cancelled, ct);

    public async Task AddAndSaveAsync(VisitorRegistration registration, CancellationToken ct = default)
    {
        db.VisitorRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
