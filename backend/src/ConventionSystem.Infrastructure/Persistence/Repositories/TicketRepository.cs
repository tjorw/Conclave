using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
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

    public async Task<IReadOnlyList<PersonTicketForReceptionDto>> ListForReceptionAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var tickets = await db.Tickets
            .Where(t => t.PersonId == personId && t.EditionId == editionId)
            .Select(t => new {
                t.Id, t.TicketTypeId, t.Status, t.FinalPrice,
                t.CollectedAt, t.CreatedAt,
                IsCollected = t.Status == TicketStatus.Collected
            })
            .ToListAsync(ct);

        if (tickets.Count == 0) return [];

        var typeIds = tickets.Select(t => t.TicketTypeId).Distinct().ToList();
        var ticketTypes = await db.TicketTypes
            .Where(tt => typeIds.Contains(tt.Id))
            .Select(tt => new { tt.Id, tt.Name, tt.Type, tt.ValidDays, tt.AllowedCategories })
            .ToDictionaryAsync(tt => tt.Id, ct);

        var perksRaw = await db.Set<TicketPerk>()
            .Where(p => typeIds.Contains(EF.Property<TicketTypeId>(p, "TicketTypeId")))
            .Select(p => new { TypeId = EF.Property<TicketTypeId>(p, "TicketTypeId"), p.Description })
            .ToListAsync(ct);

        var perksByType = perksRaw
            .GroupBy(p => p.TypeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.Description).ToList());

        return tickets.Select(t =>
        {
            ticketTypes.TryGetValue(t.TicketTypeId, out var tt);
            perksByType.TryGetValue(t.TicketTypeId, out var perks);
            return new PersonTicketForReceptionDto(
                t.Id.Value,
                t.TicketTypeId.Value,
                tt?.Name ?? "–",
                tt?.Type.ToString() ?? "–",
                t.Status.ToString(),
                t.FinalPrice,
                tt?.ValidDays,
                tt?.AllowedCategories,
                perks ?? [],
                t.IsCollected,
                t.CollectedAt,
                t.CreatedAt);
        }).ToList();
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
