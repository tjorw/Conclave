using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Convention.Abstractions;

public interface IPersonRepository
{
    Task<bool> EmailExistsInConventionAsync(ConventionId conventionId, string email, CancellationToken ct = default);
    Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default);
    Task<Person?> FindByEmailInConventionAsync(ConventionId conventionId, string email, CancellationToken ct = default);
    Task<IReadOnlyList<PersonDto>> ListByConventionIdAsync(ConventionId conventionId, CancellationToken ct = default);
    Task<IReadOnlyList<PersonSearchResultDto>> SearchForReceptionAsync(ConventionId conventionId, EditionId editionId, string searchTerm, int limit, CancellationToken ct = default);
    Task<PersonSearchResultDto?> FindByTicketIdForReceptionAsync(EditionId editionId, TicketId ticketId, CancellationToken ct = default);
    Task AddAndSaveAsync(Person person, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
