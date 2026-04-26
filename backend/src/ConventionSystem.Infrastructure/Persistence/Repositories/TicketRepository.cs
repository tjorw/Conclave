using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository(ConventionDbContext db) : ITicketRepository
{
    public Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken ct = default)
        => db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Ticket>> ListActiveOrganiserTicketsAsync(
        EditionId editionId,
        IReadOnlyCollection<PersonId> personIds,
        CancellationToken ct = default)
    {
        if (personIds.Count == 0)
            return [];

        var organiserTicketTypeIds = db.TicketTypes
            .Where(tt => tt.EditionId == editionId && tt.Type == TicketTypeCategory.Organiser)
            .Select(tt => tt.Id);

        return await db.Tickets
            .Where(t =>
                t.EditionId == editionId &&
                personIds.Contains(t.PersonId) &&
                t.Status != TicketStatus.Revoked &&
                organiserTicketTypeIds.Contains(t.TicketTypeId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Ticket>> ListActiveStaffTicketsAsync(
        EditionId editionId,
        IReadOnlyCollection<PersonId> personIds,
        CancellationToken ct = default)
    {
        if (personIds.Count == 0)
            return [];

        var staffTicketTypeIds = db.TicketTypes
            .Where(tt => tt.EditionId == editionId && tt.Type == TicketTypeCategory.Staff)
            .Select(tt => tt.Id);

        return await db.Tickets
            .Where(t =>
                t.EditionId == editionId &&
                personIds.Contains(t.PersonId) &&
                t.Status != TicketStatus.Revoked &&
                staffTicketTypeIds.Contains(t.TicketTypeId))
            .ToListAsync(ct);
    }

    public Task<bool> ExistsByTypeAsync(TicketTypeId ticketTypeId, CancellationToken ct = default)
        => db.Tickets.AnyAsync(t => t.TicketTypeId == ticketTypeId, ct);

    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public async Task AddAndSaveAsync(Ticket ticket, CancellationToken ct = default)
    {
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
