using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class SessionRegistrationRepository(ConventionDbContext db) : ISessionRegistrationRepository
{
    public Task<SessionRegistration?> GetByIdAsync(SessionRegistrationId id, CancellationToken ct = default)
        => db.SessionRegistrations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<SessionRegistration>> GetAllConfirmedBySessionIdAsync(SessionId sessionId, CancellationToken ct = default)
        => await db.SessionRegistrations
            .Where(r => r.SessionId == sessionId
                     && r.Status == Domain.Registration.Enums.SessionRegistrationStatus.Confirmed)
            .ToListAsync(ct);

    public Task<bool> HasRegistrationAsync(PersonId personId, SessionId sessionId, CancellationToken ct = default)
        => db.SessionRegistrations.AnyAsync(
            r => r.PersonId == personId && r.SessionId == sessionId
              && r.Status != Domain.Registration.Enums.SessionRegistrationStatus.Cancelled, ct);

    public async Task AddAndSaveAsync(SessionRegistration registration, CancellationToken ct = default)
    {
        db.SessionRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
