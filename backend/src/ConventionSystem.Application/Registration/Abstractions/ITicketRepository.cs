using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(TicketId id, CancellationToken ct = default);
    Task<bool> ExistsByTypeAsync(TicketTypeId ticketTypeId, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task AddAndSaveAsync(Ticket ticket, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
