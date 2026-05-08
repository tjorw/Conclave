using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface ITeamRepository
{
    Task AddAndSaveAsync(Team team, CancellationToken ct = default);
    Task<Team?> GetByIdAsync(TeamId id, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
