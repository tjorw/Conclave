using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.ListEditionStaffTicketAssignments;

public sealed record ListEditionStaffTicketAssignmentsQuery(Guid EditionId)
    : IQuery<IReadOnlyList<StaffTicketAssignmentDto>>;
