using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository(ConventionDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken ct = default)
        => db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Add(ticket);
        return Task.CompletedTask;
    }

    public async Task AddAndSaveAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
