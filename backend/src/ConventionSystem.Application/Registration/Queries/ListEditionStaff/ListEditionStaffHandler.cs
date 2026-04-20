using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.ListEditionStaff;

public sealed class ListEditionStaffHandler(IStaffApplicationRepository staffApplicationRepository)
    : IQueryHandler<ListEditionStaffQuery, IReadOnlyList<EditionStaffMemberDto>>
{
    public Task<IReadOnlyList<EditionStaffMemberDto>> Handle(ListEditionStaffQuery query, CancellationToken ct)
        => staffApplicationRepository.ListApprovedByEditionIdAsync(new EditionId(query.EditionId), ct);
}
