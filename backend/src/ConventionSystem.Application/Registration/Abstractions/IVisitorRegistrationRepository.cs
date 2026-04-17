using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface IVisitorRegistrationRepository
{
    Task<VisitorRegistration?> GetByIdAsync(VisitorRegistrationId id, CancellationToken ct = default);
    Task<bool> HasActiveRegistrationAsync(PersonId personId, EditionId editionId, CancellationToken ct = default);
    Task<bool> HasActiveRegistrationForTicketTypeAsync(PersonId personId, EditionId editionId, TicketTypeId ticketTypeId, CancellationToken ct = default);
    Task<IReadOnlyList<MyVisitorRegistrationDto>> ListByPersonAndEditionAsync(PersonId personId, EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<EditionVisitorDto>> ListConfirmedByEditionIdAsync(EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<VisitorRegistrationAdminDto>> ListByEditionAsync(EditionId editionId, CancellationToken ct = default);
    Task AddAndSaveAsync(VisitorRegistration registration, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
