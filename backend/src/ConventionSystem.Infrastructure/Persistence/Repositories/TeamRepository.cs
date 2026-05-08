using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TeamRepository(ConventionDbContext db) : ITeamRepository
{
    public Task<Team?> GetByIdAsync(TeamId id, CancellationToken ct = default)
        => db.Teams.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAndSaveAsync(Team team, CancellationToken ct = default)
    {
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
