using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class TeamEventRegistrationRepository(ConventionDbContext db) : ITeamEventRegistrationRepository
{
    public Task<TeamEventRegistration?> GetByIdAsync(TeamEventRegistrationId id, CancellationToken ct = default)
        => db.TeamEventRegistrations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> HasActiveRegistrationAsync(PersonId captainPersonId, EventId eventId, CancellationToken ct = default)
        => db.TeamEventRegistrations
            .Join(db.Teams, r => r.TeamId, t => t.Id, (r, t) => new { r, t })
            .AnyAsync(x => x.t.CaptainPersonId == captainPersonId
                        && x.r.EventId == eventId
                        && x.r.Status != TeamRegistrationStatus.Cancelled, ct);

    public async Task<IReadOnlyList<TeamRegistrationSummaryDto>> ListByEventIdAsync(EventId eventId, CancellationToken ct = default)
    {
        var results = await db.TeamEventRegistrations
            .Where(r => r.EventId == eventId)
            .Join(db.Teams, r => r.TeamId, t => t.Id, (r, t) => new { r, t })
            .Join(db.Persons, x => x.t.CaptainPersonId, p => p.Id, (x, p) => new { x.r, x.t, p })
            .OrderBy(x => x.r.CreatedAt)
            .Select(x => new TeamRegistrationSummaryDto(
                x.r.Id.Value,
                x.t.Id.Value,
                x.t.Name,
                x.t.CaptainPersonId.Value,
                x.p.Name,
                x.r.Status.ToString(),
                x.r.CreatedAt,
                x.r.UpdatedAt))
            .ToListAsync(ct);

        return (IReadOnlyList<TeamRegistrationSummaryDto>)results;
    }

    public async Task<TeamRegistrationDetailDto?> GetDetailByIdAsync(TeamEventRegistrationId id, CancellationToken ct = default)
    {
        var result = await db.TeamEventRegistrations
            .Where(r => r.Id == id)
            .Join(db.Teams, r => r.TeamId, t => t.Id, (r, t) => new { r, t })
            .Join(db.Persons, x => x.t.CaptainPersonId, p => p.Id, (x, p) => new { x.r, x.t, p })
            .Select(x => new TeamRegistrationDetailDto(
                x.r.Id.Value,
                x.t.Id.Value,
                x.t.Name,
                x.t.CaptainPersonId.Value,
                x.p.Name,
                x.r.EventId.Value,
                x.r.EditionId.Value,
                x.r.Status.ToString(),
                x.r.CreatedAt,
                x.r.UpdatedAt))
            .FirstOrDefaultAsync(ct);

        return result;
    }

    public async Task AddAndSaveAsync(TeamEventRegistration registration, CancellationToken ct = default)
    {
        db.TeamEventRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
