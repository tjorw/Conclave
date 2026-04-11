using ConventionSystem.Application.Convention.Abstractions;
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

    public async Task AddAndSaveAsync(Person person, CancellationToken ct = default)
    {
        await db.Persons.AddAsync(person, ct);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
