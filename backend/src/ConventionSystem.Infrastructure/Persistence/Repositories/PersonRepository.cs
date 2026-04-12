using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class PersonRepository(ConventionDbContext db) : IPersonRepository
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

        return await db.Persons
            .Where(p => p.ConventionId == conventionId)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone, p.IsActive })
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<PersonDto>)t.Result
                .Select(p => new PersonDto(p.Id.Value, p.Name, p.Email, p.Phone, p.IsActive, adminIds.Contains(p.Id)))
                .ToList(), TaskContinuationOptions.ExecuteSynchronously);
    }

    public async Task AddAndSaveAsync(Person person, CancellationToken ct = default)
    {
        await db.Persons.AddAsync(person, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
