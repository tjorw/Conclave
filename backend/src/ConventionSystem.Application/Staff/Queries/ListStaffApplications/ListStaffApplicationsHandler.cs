using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Staff.Queries.ListStaffApplications;

public sealed class ListStaffApplicationsHandler(IStaffApplicationRepository staffApplicationRepository)
    : IQueryHandler<ListStaffApplicationsQuery, IReadOnlyList<StaffApplicationSummaryDto>>
{
    public Task<IReadOnlyList<StaffApplicationSummaryDto>> Handle(ListStaffApplicationsQuery query, CancellationToken ct)
        => staffApplicationRepository.ListByEditionIdAsync(new EditionId(query.EditionId), ct);
}
