using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMySessionRegistrations;

public sealed class GetMySessionRegistrationsHandler(
    ISessionRegistrationRepository sessionRegistrationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMySessionRegistrationsQuery, IReadOnlyList<MySessionRegistrationSummaryDto>>
{
    public Task<IReadOnlyList<MySessionRegistrationSummaryDto>> Handle(GetMySessionRegistrationsQuery query, CancellationToken ct)
        => sessionRegistrationRepository.ListByPersonAndEditionAsync(
            currentUser.PersonId, new EditionId(query.EditionId), ct);
}
