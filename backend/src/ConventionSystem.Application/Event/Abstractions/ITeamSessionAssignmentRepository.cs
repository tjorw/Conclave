using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Abstractions;

public interface ITeamSessionAssignmentRepository
{
    Task<IReadOnlyList<TeamSessionAssignmentDto>> ListBySessionIdAsync(
        SessionId sessionId, CancellationToken ct = default);
}
