using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
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

    public Task<VisitorRegistration?> GetByTicketIdAsync(TicketId ticketId, CancellationToken ct = default)
        => db.VisitorRegistrations.FirstOrDefaultAsync(r => r.TicketId == ticketId, ct);

    public Task<bool> HasActiveRegistrationAsync(PersonId personId, EditionId editionId, CancellationToken ct = default)
        => db.VisitorRegistrations.AnyAsync(
            r => r.PersonId == personId && r.EditionId == editionId && r.Status != VisitorRegistrationStatus.Cancelled, ct);

    public Task<bool> HasActiveRegistrationForTicketTypeAsync(
        PersonId personId,
        EditionId editionId,
        TicketTypeId ticketTypeId,
        CancellationToken ct = default)
        => db.VisitorRegistrations
            .Where(r => r.PersonId == personId
                        && r.EditionId == editionId
                        && r.Status != VisitorRegistrationStatus.Cancelled)
            .Join(
                db.Tickets,
                registration => registration.TicketId,
                ticket => ticket.Id,
                (registration, ticket) => ticket)
            .AnyAsync(ticket => ticket.TicketTypeId == ticketTypeId, ct);

    public async Task<IReadOnlyList<EditionVisitorDto>> ListConfirmedByEditionIdAsync(EditionId editionId, CancellationToken ct = default)
    {
        var registrations = await db.VisitorRegistrations
            .Where(r => r.EditionId == editionId && r.Status == VisitorRegistrationStatus.Confirmed)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        var personIds = registrations.Select(r => r.PersonId).Distinct().ToHashSet();
        var personMap = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone })
            .ToDictionaryAsync(p => p.Id, ct);

        return registrations.Select(r =>
        {
            personMap.TryGetValue(r.PersonId, out var p);
            return new EditionVisitorDto(r.PersonId.Value, p?.Name ?? "", p?.Email ?? "", p?.Phone);
        }).ToList();
    }

    public async Task<IReadOnlyList<MyVisitorRegistrationDto>> ListByPersonAndEditionAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var registrations = await db.VisitorRegistrations
            .Where(r => r.PersonId == personId
                        && r.EditionId == editionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (registrations.Count == 0)
            return [];

        var ticketIds = registrations.Select(r => r.TicketId).Distinct().ToHashSet();
        var ticketMap = await db.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .Select(t => new { t.Id, t.TicketTypeId, t.FinalPrice, t.Status })
            .ToDictionaryAsync(t => t.Id, ct);

        var ticketTypeIds = ticketMap.Values.Select(t => t.TicketTypeId).Distinct().ToHashSet();
        var ticketTypeMap = await db.TicketTypes
            .Where(tt => ticketTypeIds.Contains(tt.Id))
            .Select(tt => new { tt.Id, tt.Name, tt.Price, tt.Type, tt.Description, tt.ValidDays })
            .ToDictionaryAsync(tt => tt.Id, ct);

        return registrations.Select(registration =>
        {
            ticketMap.TryGetValue(registration.TicketId, out var ticket);
            var ticketType = ticket is not null && ticketTypeMap.TryGetValue(ticket.TicketTypeId, out var tt) ? tt : null;

            var ticketPrice = ticket?.FinalPrice ?? ticketType?.Price;
            var canCancel =
                ticketType?.Type == TicketTypeCategory.Visitor &&
                (registration.Status == VisitorRegistrationStatus.PendingPayment ||
                 (registration.Status == VisitorRegistrationStatus.Confirmed && ticketPrice == 0));

            return new MyVisitorRegistrationDto(
                registration.Id.Value,
                registration.Status.ToString(),
                ticketType?.Name,
                registration.TicketId.Value,
                ticketPrice,
                ticketType?.Type.ToString() ?? "",
                ticket?.Status.ToString() ?? "",
                ticketType?.Description,
                ticketType?.ValidDays,
                canCancel);
        }).ToList();
    }

    public async Task<IReadOnlyList<VisitorRegistrationAdminDto>> ListByEditionAsync(
        EditionId editionId, CancellationToken ct = default)
    {
        var registrations = await db.VisitorRegistrations
            .Where(r => r.EditionId == editionId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        var personIds = registrations.Select(r => r.PersonId).Distinct().ToHashSet();
        var personMap = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, ct);

        var ticketIds = registrations.Select(r => r.TicketId).Distinct().ToHashSet();
        var ticketMap = await db.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .Select(t => new { t.Id, t.TicketTypeId })
            .ToDictionaryAsync(t => t.Id, ct);

        var ticketTypeIds = ticketMap.Values.Select(t => t.TicketTypeId).Distinct().ToHashSet();
        var ticketTypeMap = await db.TicketTypes
            .Where(tt => ticketTypeIds.Contains(tt.Id))
            .Select(tt => new { tt.Id, tt.Name })
            .ToDictionaryAsync(tt => tt.Id, ct);

        return registrations.Select(r =>
        {
            personMap.TryGetValue(r.PersonId, out var person);
            ticketMap.TryGetValue(r.TicketId, out var ticket);
            string? ticketTypeName = null;
            if (ticket is not null)
            {
                ticketTypeMap.TryGetValue(ticket.TicketTypeId, out var tt);
                ticketTypeName = tt?.Name;
            }

            return new VisitorRegistrationAdminDto(
                r.Id.Value,
                r.PersonId.Value,
                person?.Name ?? "",
                ticketTypeName,
                r.Status.ToString(),
                r.CreatedAt,
                r.PaymentReference);
        }).ToList();
    }

    public async Task AddAndSaveAsync(VisitorRegistration registration, CancellationToken ct = default)
    {
        db.VisitorRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
