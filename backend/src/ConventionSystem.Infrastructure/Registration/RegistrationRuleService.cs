using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;
using ConventionSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConventionSystem.Infrastructure.Registration;

public sealed class RegistrationRuleService(ConventionDbContext db) : IRegistrationRuleService
{
    // Seat availability is still outside the current scope and handled as permissive.
    public bool ValidateSeatAvailability(SessionId sessionId) => true;

    public bool ValidateTicket(TicketId ticketId, SessionId sessionId)
    {
        var sessionInfo = db.Events
            .AsNoTracking()
            .SelectMany(e => e.Sessions.Select(s => new
            {
                SessionId = s.Id,
                SessionStatus = s.Status,
                SessionStart = s.TimeSlot.Start,
                EditionId = e.EditionId,
                CategoryId = e.CategoryId
            }))
            .FirstOrDefault(x => x.SessionId == sessionId);

        if (sessionInfo is null || sessionInfo.SessionStatus != SessionStatus.Active)
            return false;

        var ticketInfo = db.Tickets
            .AsNoTracking()
            .Where(t => t.Id == ticketId)
            .Select(t => new
            {
                t.Status,
                t.EditionId,
                t.TicketTypeId
            })
            .FirstOrDefault();

        if (ticketInfo is null)
            return false;

        if (ticketInfo.Status != TicketStatus.Paid && ticketInfo.Status != TicketStatus.Collected)
            return false;

        if (ticketInfo.EditionId != sessionInfo.EditionId)
            return false;

        var ticketType = db.TicketTypes
            .AsNoTracking()
            .Where(tt => tt.Id == ticketInfo.TicketTypeId)
            .Select(tt => new
            {
                tt.ValidDays,
                tt.AllowedCategories
            })
            .FirstOrDefault();

        if (ticketType is null)
            return false;

        var sessionDate = DateOnly.FromDateTime(sessionInfo.SessionStart);

        if (ticketType.ValidDays is { Count: > 0 } && !ticketType.ValidDays.Contains(sessionDate))
            return false;

        if (ticketType.AllowedCategories is { Length: > 0 } && !ticketType.AllowedCategories.Contains(sessionInfo.CategoryId.Value))
            return false;

        return true;
    }
}
