using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ITicketTypeRepository
{
    Task<TicketType?> GetByIdAsync(TicketTypeId id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketTypeAdminDto>> ListByEditionIdAsync(EditionId editionId, CancellationToken ct = default);
    Task AddAndSaveAsync(TicketType ticketType, CancellationToken ct = default);
    Task DeleteAndSaveAsync(TicketType ticketType, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
