using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(ConventionDbContext db) : IEventRepository
{
    public async Task AddAndSaveAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
    }

    public Task<Domain.Event.Aggregates.Event?> GetByIdAsync(EventId id, CancellationToken ct = default)
        => db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.CoOrganisers)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithInvitationsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.CoOrganiserInvitations)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithCoOrganisersAndInvitationsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.CoOrganisers)
            .Include(e => e.CoOrganiserInvitations)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByInvitationCodeAsync(string code, CancellationToken ct = default)
        => db.Events
            .Include(e => e.CoOrganisers)
            .Include(e => e.CoOrganiserInvitations)
            .FirstOrDefaultAsync(e => e.CoOrganiserInvitations.Any(i => i.Code == code), ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Sessions)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithSessionsAndTeamAssignmentsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Sessions)
                .ThenInclude(s => s.TeamAssignments)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithCommentsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Comments)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithCommentsAndCoOrganisersAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Comments)
            .Include(e => e.CoOrganisers)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Domain.Event.Aggregates.Event?> GetByIdWithTranslationsAsync(EventId id, CancellationToken ct = default)
        => db.Events
            .Include(e => e.Translations)
            .Include(e => e.CoOrganisers)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<EventTranslation?> GetTranslationAsync(EventId id, string locale, CancellationToken ct = default)
        => db.Set<EventTranslation>()
            .FirstOrDefaultAsync(t => EF.Property<EventId>(t, "EventId") == id && t.Locale == locale.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<EventSummaryDto>> ListByEditionIdAsync(EditionId id, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Sessions)
            .Include(e => e.Comments)
            .Where(e => e.EditionId == id)
            .ToListAsync(ct);

        var categoryNames = await db.Categories
            .Where(c => EF.Property<EditionId>(c, "EditionId") == id)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var organiserIds = events.Select(e => e.LeadOrganiserId).Distinct().ToHashSet();
        var organiserNames = await db.Persons
            .Where(p => organiserIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return events.Select(e => new EventSummaryDto(
            e.Id.Value,
            e.EditionId.Value,
            e.CategoryId.Value,
            categoryNames.GetValueOrDefault(e.CategoryId),
            e.LeadOrganiserId.Value,
            organiserNames.GetValueOrDefault(e.LeadOrganiserId),
            e.Status.ToString(),
            string.IsNullOrEmpty(e.Title) ? null : e.Title,
            e.IsFeatured,
            e.FeaturedSortOrder,
            e.Sessions.Count(s => s.Status == Domain.Event.Enums.SessionStatus.Active),
            e.Comments.Count(c => c.RequiresHandling && (c.Status == EventCommentStatus.New || c.Status == EventCommentStatus.InProgress)),
            e.Description ?? "",
            e.ProgramTags.Select(t => t.Name).ToList(),
            e.Sessions.Select(s => new SessionSummaryDto(
                s.Id.Value,
                s.VenueId.Value,
                s.TimeSlot.Start,
                s.TimeSlot.End,
                s.MaxSeats,
                s.StartType.ToString(),
                s.Status.ToString()
            )).ToList()
        )).ToList();
    }

    public Task<int> CountFeaturedByEditionIdAsync(EditionId id, CancellationToken ct = default)
        => db.Events.CountAsync(e => e.EditionId == id && e.IsFeatured, ct);

    public async Task<EventDto?> GetProjectedByIdAsync(EventId id, CancellationToken ct = default)
    {
        var ev = await db.Events
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .Include(e => e.Comments)
            .Include(e => e.CoOrganiserInvitations)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (ev is null) return null;

        var category = await db.Categories
            .Where(c => c.Id == ev.CategoryId)
            .Select(c => new { c.Name, c.ResponsibleId, c.OrganizerInstructions })
            .FirstOrDefaultAsync(ct);

        var sessionIds = ev.Sessions.Select(s => s.Id).ToList();
        var venueIds = ev.Sessions.Select(s => s.VenueId).Distinct().ToList();

        var venueNames = await db.Venues
            .Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id.Value, v => v.Name, ct);

        var registrationCounts = sessionIds.Count > 0
            ? await db.SessionRegistrations
                .Where(r => sessionIds.Contains(r.SessionId)
                         && r.Status == SessionRegistrationStatus.Confirmed)
                .GroupBy(r => r.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SessionId.Value, x => x.Count, ct)
            : new Dictionary<Guid, int>();

        var pendingCounts = sessionIds.Count > 0
            ? await db.SessionRegistrations
                .Where(r => sessionIds.Contains(r.SessionId)
                         && r.Status == SessionRegistrationStatus.Pending)
                .GroupBy(r => r.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SessionId.Value, x => x.Count, ct)
            : new Dictionary<Guid, int>();

        var personIds = new List<PersonId> { ev.LeadOrganiserId };
        personIds.AddRange(ev.CoOrganisers.Select(c => c.PersonId));
        if (category is not null) personIds.Add(category.ResponsibleId);
        foreach (var comment in ev.Comments)
        {
            personIds.Add(comment.AuthorId);
            if (comment.HandledById is not null) personIds.Add(comment.HandledById.Value);
        }

        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id.Value, p => p.Name, ct);

        var organiserName = personNames.GetValueOrDefault(ev.LeadOrganiserId.Value);
        var responsibleName = category is not null
            ? personNames.GetValueOrDefault(category.ResponsibleId.Value)
            : null;

        return new EventDto(
            ev.Id.Value,
            ev.EditionId.Value,
            ev.CategoryId.Value,
            category?.Name,
            category is not null ? category.ResponsibleId.Value : (Guid?)null,
            responsibleName,
            category?.OrganizerInstructions,
            ev.LeadOrganiserId.Value,
            organiserName,
            ev.Status.ToString(),
            ev.Title,
            ev.Description,
            ev.ProgramTags.Select(t => t.Name).ToList(),
            ev.ScheduleRequestText,
            ev.RegistrationType.ToString(),
            ev.RegistrationMode.ToString(),
            ev.TeamSize?.Min,
            ev.TeamSize?.Max,
            ev.DropInRules,
            ev.IsFeatured,
            ev.FeaturedSortOrder,
            ev.CoOrganisers.Select(c => c.PersonId.Value).ToList(),
            ev.CoOrganisers.Select(c => new CoOrganiserDto(
                c.PersonId.Value,
                personNames.GetValueOrDefault(c.PersonId.Value))).ToList(),
            ev.Sessions.Select(s => new SessionDto(
                s.Id.Value, s.VenueId.Value,
                venueNames.GetValueOrDefault(s.VenueId.Value),
                s.TimeSlot.Start, s.TimeSlot.End,
                s.MaxSeats,
                registrationCounts.GetValueOrDefault(s.Id.Value),
                pendingCounts.GetValueOrDefault(s.Id.Value),
                s.StartType.ToString(), s.Status.ToString())).ToList(),
            ev.Comments.Select(c => new EventCommentDto(
                c.Id.Value,
                c.AuthorId.Value,
                personNames.GetValueOrDefault(c.AuthorId.Value),
                c.Text,
                c.Status.ToString(),
                c.RequiresHandling,
                c.HandlingComment,
                c.HandledById?.Value,
                c.HandledById is not null ? personNames.GetValueOrDefault(c.HandledById.Value.Value) : null,
                c.HandledAt,
                c.AcknowledgedById?.Value,
                c.AcknowledgedAt,
                c.CreatedAt)).ToList(),
            ev.CoOrganiserCount,
            ev.CoOrganiserLimit,
            ev.CoOrganiserInvitations.Select(i => new CoOrganiserInvitationDto(
                i.Id.Value,
                i.Email,
                i.CreatedAt)).ToList(),
            ev.AllocationMode.ToString());
    }

    public async Task<IReadOnlyList<EventSummaryDto>> ListByEditionAndOrganiserAsync(
        EditionId editionId, PersonId organiserId, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Sessions)
            .Include(e => e.CoOrganisers)
            .Include(e => e.Comments)
            .Where(e => e.EditionId == editionId &&
                        (e.LeadOrganiserId == organiserId ||
                         e.CoOrganisers.Any(c => c.PersonId == organiserId)))
            .ToListAsync(ct);

        var categoryNames = await db.Categories
            .Where(c => EF.Property<EditionId>(c, "EditionId") == editionId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return events.Select(e => new EventSummaryDto(
            e.Id.Value,
            e.EditionId.Value,
            e.CategoryId.Value,
            categoryNames.GetValueOrDefault(e.CategoryId),
            e.LeadOrganiserId.Value,
            null,
            e.Status.ToString(),
            string.IsNullOrEmpty(e.Title) ? null : e.Title,
            e.IsFeatured,
            e.FeaturedSortOrder,
            e.Sessions.Count(s => s.Status == Domain.Event.Enums.SessionStatus.Active),
            e.Comments.Count(c => c.RequiresHandling && (c.Status == EventCommentStatus.New || c.Status == EventCommentStatus.InProgress)),
            e.Description ?? "",
            e.ProgramTags.Select(t => t.Name).ToList(),
            e.Sessions.Select(s => new SessionSummaryDto(
                s.Id.Value,
                s.VenueId.Value,
                s.TimeSlot.Start,
                s.TimeSlot.End,
                s.MaxSeats,
                s.StartType.ToString(),
                s.Status.ToString()
            )).ToList()
        )).ToList();
    }

    public async Task<IReadOnlyList<EditionOrganiserDto>> ListOrganisersByEditionIdAsync(EditionId editionId, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.CoOrganisers)
            .Where(e => e.EditionId == editionId && e.Status != Domain.Event.Enums.EventStatus.Cancelled)
            .OrderBy(e => e.Title)
            .ToListAsync(ct);

        var personIds = new HashSet<Domain.Convention.Ids.PersonId>();
        foreach (var ev in events)
        {
            personIds.Add(ev.LeadOrganiserId);
            foreach (var co in ev.CoOrganisers) personIds.Add(co.PersonId);
        }

        var personMap = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Email, p.Phone })
            .ToDictionaryAsync(p => p.Id, ct);

        var result = new List<EditionOrganiserDto>();
        foreach (var ev in events)
        {
            personMap.TryGetValue(ev.LeadOrganiserId, out var lead);
            result.Add(new EditionOrganiserDto(
                ev.LeadOrganiserId.Value,
                lead?.Name ?? "",
                lead?.Email ?? "",
                lead?.Phone,
                ev.Id.Value,
                ev.Title,
                "Huvudarrangör"));

            foreach (var co in ev.CoOrganisers)
            {
                personMap.TryGetValue(co.PersonId, out var coP);
                result.Add(new EditionOrganiserDto(
                    co.PersonId.Value,
                    coP?.Name ?? "",
                    coP?.Email ?? "",
                    coP?.Phone,
                    ev.Id.Value,
                    ev.Title,
                    "Medarrangör"));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<EditionSessionDto>> ListSessionsByEditionIdAsync(EditionId editionId, CancellationToken ct = default)
    {
        var events = await db.Events
            .Include(e => e.Sessions)
            .Where(e => e.EditionId == editionId)
            .ToListAsync(ct);

        return events
            .SelectMany(e => e.Sessions, (e, s) => new EditionSessionDto(
                s.Id.Value,
                e.Id.Value,
                e.Title ?? "",
                s.VenueId.Value,
                s.TimeSlot.Start,
                s.TimeSlot.End,
                s.MaxSeats,
                s.StartType.ToString(),
                s.Status.ToString()))
            .OrderBy(s => s.Start)
            .ToList();
    }

    public async Task<SessionAllocationInfoDto?> GetSessionAllocationInfoAsync(SessionId sessionId, CancellationToken ct = default)
    {
        var result = await db.Events
            .Where(e => e.Sessions.Any(s => s.Id == sessionId))
            .Select(e => new
            {
                e.AllocationMode,
                MaxSeats = e.Sessions.Where(s => s.Id == sessionId).Select(s => s.MaxSeats).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        return result is null ? null : new SessionAllocationInfoDto(result.AllocationMode, result.MaxSeats);
    }

    public async Task DeleteAsync(Domain.Event.Aggregates.Event ev, CancellationToken ct = default)
    {
        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
