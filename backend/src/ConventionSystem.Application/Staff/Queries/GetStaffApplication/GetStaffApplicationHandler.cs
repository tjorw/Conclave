using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Staff.Queries.GetStaffApplication;

public sealed class GetStaffApplicationHandler(IStaffApplicationRepository staffApplicationRepository)
    : IQueryHandler<GetStaffApplicationQuery, StaffApplicationSummaryDto?>
{
    public Task<StaffApplicationSummaryDto?> Handle(GetStaffApplicationQuery query, CancellationToken ct)
        => staffApplicationRepository.GetSummaryByIdAsync(new StaffApplicationId(query.ApplicationId), ct);
}
