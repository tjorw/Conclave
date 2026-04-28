using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Staff.Queries.GetStaffApplication;

public sealed record GetStaffApplicationQuery(Guid ApplicationId)
    : IQuery<StaffApplicationSummaryDto?>;
