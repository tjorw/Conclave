using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TeamSessionAssignmentRepository(ConventionDbContext db) : ITeamSessionAssignmentRepository
{
    public async Task<IReadOnlyList<TeamSessionAssignmentDto>> ListBySessionIdAsync(
        SessionId sessionId, CancellationToken ct = default)
    {
        var assignments = await db.TeamSessionAssignments
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(ct);

        if (assignments.Count == 0)
            return [];

        var regIds = assignments
            .Select(a => new TeamEventRegistrationId(a.TeamEventRegistrationId))
            .ToList();

        var regInfos = await db.TeamEventRegistrations
            .Where(r => regIds.Contains(r.Id))
            .Join(db.Teams, r => r.TeamId, t => t.Id, (r, t) => new { r, t })
            .Join(db.Persons, x => x.t.CaptainPersonId, p => p.Id, (x, p) => new
            {
                RegistrationIdGuid = x.r.Id.Value,
                TeamName = x.t.Name,
                CaptainPersonIdGuid = x.t.CaptainPersonId.Value,
                CaptainName = p.Name
            })
            .ToListAsync(ct);

        var infoMap = regInfos.ToDictionary(d => d.RegistrationIdGuid);

        return assignments
            .Where(a => infoMap.ContainsKey(a.TeamEventRegistrationId))
            .Select(a =>
            {
                var info = infoMap[a.TeamEventRegistrationId];
                return new TeamSessionAssignmentDto(
                    a.TeamEventRegistrationId,
                    info.TeamName,
                    info.CaptainPersonIdGuid,
                    info.CaptainName,
                    a.AssignedAt);
            })
            .ToList();
    }
}
