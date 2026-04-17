using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;

public sealed class GetMyVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyVisitorRegistrationQuery, IReadOnlyList<MyVisitorRegistrationDto>>
{
    public Task<IReadOnlyList<MyVisitorRegistrationDto>> Handle(GetMyVisitorRegistrationQuery query, CancellationToken ct)
        => visitorRegistrationRepository.ListByPersonAndEditionAsync(
            currentUser.PersonId, new EditionId(query.EditionId), ct);
}
