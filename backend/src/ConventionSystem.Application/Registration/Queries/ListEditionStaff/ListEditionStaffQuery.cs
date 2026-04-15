using ConventionSystem.Application.Common;
using ConventionSystem.Application.Staff.Queries;

namespace ConventionSystem.Application.Registration.Queries.ListEditionStaff;

public sealed record ListEditionStaffQuery(Guid EditionId)
    : IQuery<IReadOnlyList<EditionStaffMemberDto>>;
