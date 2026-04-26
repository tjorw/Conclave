using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken ct = default);
    Task<IReadOnlyList<Ticket>> ListActiveOrganiserTicketsAsync(EditionId editionId, IReadOnlyCollection<PersonId> personIds, CancellationToken ct = default);
    Task<bool> ExistsByTypeAsync(TicketTypeId ticketTypeId, CancellationToken ct = default);
    void Add(Ticket ticket);
    Task AddAndSaveAsync(Ticket ticket, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
