using ConventionSystem.Application.Common;
using ConventionSystem.Application.Staff.Queries;

namespace ConventionSystem.Application.Staff.Queries.ListStaffApplications;

public sealed record ListStaffApplicationsQuery(Guid EditionId)
    : IQuery<IReadOnlyList<StaffApplicationSummaryDto>>;
