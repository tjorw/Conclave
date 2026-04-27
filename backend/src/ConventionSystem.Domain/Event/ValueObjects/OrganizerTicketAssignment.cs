using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Event.ValueObjects;

public sealed record OrganizerTicketAssignment(PersonId PersonId, TicketTypeId? TicketTypeId);
