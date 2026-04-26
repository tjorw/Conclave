using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class PersonRepository(ConventionDbContext db, ApplicationIdentityDbContext identityDb) : IPersonRepository
{
    public Task<bool> EmailExistsInConventionAsync(ConventionId conventionId, string email, CancellationToken ct = default)
        => db.Persons.AnyAsync(p => p.ConventionId == conventionId && p.Email == email, ct);

    public Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default)
        => db.Persons.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Person?> FindByEmailInConventionAsync(ConventionId conventionId, string email, CancellationToken ct = default)
        => db.Persons.FirstOrDefaultAsync(p => p.ConventionId == conventionId && p.Email == email, ct);

    public async Task<IReadOnlyList<PersonDto>> ListByConventionIdAsync(ConventionId conventionId, CancellationToken ct = default)
    {
        var adminIds = await db.Set<ConventionAdministrator>()
            .Where(a => EF.Property<ConventionId>(a, "ConventionId") == conventionId)
            .Select(a => a.PersonId)
            .ToHashSetAsync(ct);

        var persons = await db.Persons
            .Where(p => p.ConventionId == conventionId)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone, p.IsActive })
            .ToListAsync(ct);

        var personIdValues = persons.Select(p => p.Id.Value).ToList();

        var accountMap = await identityDb.Users
            .Where(u => u.PersonId != null && personIdValues.Contains(u.PersonId!.Value))
            .Select(u => new
            {
                PersonId = u.PersonId!.Value,
                IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            })
            .ToDictionaryAsync(u => u.PersonId, ct);

        return persons
            .Select(p =>
            {
                var hasAccount = accountMap.TryGetValue(p.Id.Value, out var acc);
                return new PersonDto(
                    p.Id.Value, p.Name, p.Email, p.Phone, p.IsActive,
                    adminIds.Contains(p.Id),
                    hasAccount,
                    hasAccount && (acc?.IsLocked ?? false));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<PersonSearchResultDto>> SearchForReceptionAsync(
        ConventionId conventionId, EditionId editionId, string searchTerm, int limit, CancellationToken ct = default)
    {
        var lower = searchTerm.ToLower();
        var persons = await db.Persons
            .Where(p => p.ConventionId == conventionId && p.IsActive &&
                        (p.Name.ToLower().Contains(lower) || p.Email.ToLower().Contains(lower)))
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone })
            .ToListAsync(ct);

        return await BuildSearchResultsAsync(persons.Select(p => (p.Id, p.Name, p.Email, p.Phone)).ToList(), editionId, ct);
    }

    public async Task<PersonSearchResultDto?> FindByTicketIdForReceptionAsync(
        EditionId editionId, TicketId ticketId, CancellationToken ct = default)
    {
        var ticket = await db.Tickets
            .Where(t => t.Id == ticketId && t.EditionId == editionId)
            .Select(t => new { t.PersonId })
            .FirstOrDefaultAsync(ct);
        if (ticket == null) return null;

        var person = await db.Persons
            .Where(p => p.Id == ticket.PersonId)
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone })
            .FirstOrDefaultAsync(ct);
        if (person == null) return null;

        var results = await BuildSearchResultsAsync([(person.Id, person.Name, person.Email, person.Phone)], editionId, ct);
        return results.Count > 0 ? results[0] : null;
    }

    private async Task<IReadOnlyList<PersonSearchResultDto>> BuildSearchResultsAsync(
        IReadOnlyList<(PersonId Id, string Name, string Email, string? Phone)> persons,
        EditionId editionId, CancellationToken ct)
    {
        if (persons.Count == 0) return [];

        var personIds = persons.Select(p => p.Id).ToList();

        var tickets = await db.Tickets
            .Where(t => t.EditionId == editionId && personIds.Contains(t.PersonId))
            .Select(t => new { t.Id, t.PersonId, t.Status, t.TicketTypeId })
            .ToListAsync(ct);

        var typeIds = tickets.Select(t => t.TicketTypeId).Distinct().ToList();
        var typeNames = await db.TicketTypes
            .Where(tt => typeIds.Contains(tt.Id))
            .Select(tt => new { tt.Id, tt.Name })
            .ToDictionaryAsync(tt => tt.Id, tt => tt.Name, ct);

        var ticketsByPerson = tickets
            .GroupBy(t => t.PersonId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<TicketSummaryForReceptionDto>)g
                    .Select(t => new TicketSummaryForReceptionDto(
                        t.Id.Value,
                        typeNames.TryGetValue(t.TicketTypeId, out var n) ? n : "–",
                        t.Status.ToString()))
                    .ToList());

        return persons.Select(p => new PersonSearchResultDto(
            p.Id.Value, p.Name, p.Email, p.Phone,
            ticketsByPerson.TryGetValue(p.Id, out var ts) ? ts : []
        )).ToList();
    }

    public async Task AddAndSaveAsync(Person person, CancellationToken ct = default)
    {
        await db.Persons.AddAsync(person, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
