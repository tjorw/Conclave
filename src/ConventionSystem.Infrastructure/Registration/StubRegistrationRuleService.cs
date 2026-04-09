using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;

namespace ConventionSystem.Infrastructure.Registration;

/// <summary>
/// Temporär stub tills Event-kontexten är implementerad.
/// Returnerar alltid att plats finns och biljett är giltig.
/// </summary>
public sealed class StubRegistrationRuleService : IRegistrationRuleService
{
    public bool ValidateSeatAvailability(SessionId sessionId) => true;

    public bool ValidateTicket(TicketId ticketId, SessionId sessionId) => true;
}
