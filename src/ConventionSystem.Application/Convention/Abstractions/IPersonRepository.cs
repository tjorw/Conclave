using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IPersonRepository
{
    Task<bool> EmailExistsInConventionAsync(ConventionId conventionId, string email, CancellationToken ct = default);
    Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default);
    Task<Person?> FindByEmailInConventionAsync(ConventionId conventionId, string email, CancellationToken ct = default);
    Task AddAndSaveAsync(Person person, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
