using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
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

    public async Task<IReadOnlyList<MyVisitorRegistrationDto>> ListByPersonAndEditionAsync(
        PersonId personId,
        EditionId editionId,
        CancellationToken ct = default)
    {
        var tickets = await db.Tickets
            .Where(t => t.PersonId == personId
                        && t.EditionId == editionId
                        && t.Status != TicketStatus.Revoked)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        if (tickets.Count == 0)
            return [];

        var ticketIds = tickets.Select(t => t.Id).ToHashSet();
        var registrations = await db.VisitorRegistrations
            .Where(r => ticketIds.Contains(r.TicketId))
            .ToDictionaryAsync(r => r.TicketId, ct);

        var ticketTypeIds = tickets.Select(t => t.TicketTypeId).Distinct().ToHashSet();
        var ticketTypes = await db.TicketTypes
            .Where(tt => ticketTypeIds.Contains(tt.Id))
            .ToDictionaryAsync(tt => tt.Id, ct);

        return tickets.Select(ticket =>
        {
            registrations.TryGetValue(ticket.Id, out var registration);
            ticketTypes.TryGetValue(ticket.TicketTypeId, out var ticketType);

            var category = ticketType?.Type.ToString() ?? "";
            var price = ticket.FinalPrice ?? ticketType?.Price;
            var status = registration?.Status.ToString() ?? ticket.Status.ToString();
            var isFreeConfirmedVisitorRegistration =
                registration?.Status == VisitorRegistrationStatus.Confirmed &&
                price == 0 &&
                ticketType?.Type == TicketTypeCategory.Visitor;

            var canCancel =
                ticketType?.Type == TicketTypeCategory.Visitor &&
                registration is not null &&
                (registration.Status == VisitorRegistrationStatus.PendingPayment || isFreeConfirmedVisitorRegistration);

            return new MyVisitorRegistrationDto(
                registration?.Id.Value ?? ticket.Id.Value,
                status,
                ticketType?.Name,
                ticket.Id.Value,
                price,
                category,
                ticket.Status.ToString(),
                ticketType?.Description,
                ticketType?.ValidDays,
                canCancel);
        }).ToList();
    }

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

    public Task<bool> ExistsByTypeAsync(TicketTypeId ticketTypeId, CancellationToken ct = default)
        => db.Tickets.AnyAsync(t => t.TicketTypeId == ticketTypeId, ct);

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
