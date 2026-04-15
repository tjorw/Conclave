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

    public Task<bool> HasActiveRegistrationAsync(PersonId personId, EditionId editionId, CancellationToken ct = default)
        => db.VisitorRegistrations.AnyAsync(
            r => r.PersonId == personId && r.EditionId == editionId && r.Status != VisitorRegistrationStatus.Cancelled, ct);

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

    public async Task<MyVisitorRegistrationDto?> GetByPersonAndEditionAsync(
        PersonId personId, EditionId editionId, CancellationToken ct = default)
    {
        var registration = await db.VisitorRegistrations
            .FirstOrDefaultAsync(r => r.PersonId == personId && r.EditionId == editionId
                                      && r.Status != VisitorRegistrationStatus.Cancelled, ct);

        if (registration is null) return null;

        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == registration.TicketId, ct);
        string? ticketTypeName = null;
        if (ticket is not null)
        {
            var ticketType = await db.TicketTypes.FirstOrDefaultAsync(tt => tt.Id == ticket.TicketTypeId, ct);
            ticketTypeName = ticketType?.Name;
        }

        return new MyVisitorRegistrationDto(registration.Id.Value, registration.Status.ToString(), ticketTypeName);
    }

    public async Task AddAndSaveAsync(VisitorRegistration registration, CancellationToken ct = default)
    {
        db.VisitorRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
