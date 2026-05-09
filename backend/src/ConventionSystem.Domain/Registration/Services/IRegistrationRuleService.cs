using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Services;

public interface IRegistrationRuleService
{
    /// <summary>Kontrollerar att biljetten är giltig för den upplaga som sessionen tillhör.</summary>
    bool ValidateTicket(TicketId ticketId, SessionId sessionId);
}
