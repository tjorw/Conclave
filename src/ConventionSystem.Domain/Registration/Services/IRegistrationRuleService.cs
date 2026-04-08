using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Services;

public interface IRegistrationRuleService
{
    /// <summary>Kontrollerar om det finns lediga platser på sessionen (frågar Event-kontexten).</summary>
    bool ValidateSeatAvailability(SessionId sessionId);

    /// <summary>Kontrollerar att biljetten är giltig för den upplaga som sessionen tillhör.</summary>
    bool ValidateTicket(TicketId ticketId, SessionId sessionId);
}
