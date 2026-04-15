using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TicketTypeRepository(ConventionDbContext db) : ITicketTypeRepository
{
    public Task<TicketType?> GetByIdAsync(TicketTypeId id, CancellationToken ct = default)
        => db.TicketTypes.Include(t => t.Perks).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TicketTypeAdminDto>> ListByEditionIdAsync(EditionId editionId, CancellationToken ct = default)
    {
        return await db.TicketTypes
            .Where(t => t.EditionId == editionId)
            .OrderBy(t => t.Name)
            .Select(t => new TicketTypeAdminDto(
                t.Id.Value,
                t.Name,
                t.Price,
                t.Type.ToString(),
                t.IsSellable,
                t.IsPubliclyVisible))
            .ToListAsync(ct);
    }

    public async Task AddAndSaveAsync(TicketType ticketType, CancellationToken ct = default)
    {
        db.TicketTypes.Add(ticketType);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAndSaveAsync(TicketType ticketType, CancellationToken ct = default)
    {
        db.TicketTypes.Remove(ticketType);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
