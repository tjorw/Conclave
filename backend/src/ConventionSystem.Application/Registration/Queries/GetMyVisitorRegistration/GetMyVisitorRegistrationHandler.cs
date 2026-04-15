using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;

public sealed class GetMyVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyVisitorRegistrationQuery, MyVisitorRegistrationDto?>
{
    public Task<MyVisitorRegistrationDto?> Handle(GetMyVisitorRegistrationQuery query, CancellationToken ct)
        => visitorRegistrationRepository.GetByPersonAndEditionAsync(
            currentUser.PersonId, new EditionId(query.EditionId), ct);
}
