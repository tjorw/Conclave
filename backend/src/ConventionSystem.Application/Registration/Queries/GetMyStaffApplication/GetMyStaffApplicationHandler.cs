using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMyStaffApplication;

public sealed class GetMyStaffApplicationHandler(
    IStaffApplicationRepository staffApplicationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyStaffApplicationQuery, MyStaffApplicationDto?>
{
    public Task<MyStaffApplicationDto?> Handle(GetMyStaffApplicationQuery query, CancellationToken ct)
        => staffApplicationRepository.GetByPersonAndEditionAsync(
            currentUser.PersonId, new EditionId(query.EditionId), ct);
}
