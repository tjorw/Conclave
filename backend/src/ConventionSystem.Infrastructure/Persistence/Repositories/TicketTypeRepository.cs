using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TicketTypeRepository(ConventionDbContext db) : ITicketTypeRepository
{
    public Task<TicketType?> GetByIdAsync(TicketTypeId id, CancellationToken ct = default)
        => db.TicketTypes.Include(t => t.Perks).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAndSaveAsync(TicketType ticketType, CancellationToken ct = default)
    {
        db.TicketTypes.Add(ticketType);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
