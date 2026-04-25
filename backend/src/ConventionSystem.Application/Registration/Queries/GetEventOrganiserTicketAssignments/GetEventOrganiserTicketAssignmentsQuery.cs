using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetEventOrganiserTicketAssignments;

public sealed record GetEventOrganiserTicketAssignmentsQuery(Guid EventId) : IQuery<IReadOnlyList<OrganiserTicketAssignmentDto>>;
