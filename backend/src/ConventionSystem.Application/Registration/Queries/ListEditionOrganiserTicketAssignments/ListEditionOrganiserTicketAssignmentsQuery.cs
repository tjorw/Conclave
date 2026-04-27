using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListEditionOrganiserTicketAssignments;

public sealed record ListEditionOrganiserTicketAssignmentsQuery(Guid EditionId) : IQuery<IReadOnlyList<OrganiserTicketAssignmentDto>>;
