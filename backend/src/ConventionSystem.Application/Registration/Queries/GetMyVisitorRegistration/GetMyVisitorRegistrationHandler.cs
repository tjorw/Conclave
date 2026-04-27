using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;

public sealed class GetMyVisitorRegistrationHandler(
    IVisitorRegistrationRepository visitorRegistrationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyVisitorRegistrationQuery, IReadOnlyList<MyVisitorRegistrationDto>>
{
    public async Task<IReadOnlyList<MyVisitorRegistrationDto>> Handle(GetMyVisitorRegistrationQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var personId = currentUser.PersonId;

        var visitorRegistrations = await visitorRegistrationRepository.ListByPersonAndEditionAsync(personId, editionId, ct);
        var assignedTickets = await visitorRegistrationRepository.ListAssignedTicketsByPersonAndEditionAsync(personId, editionId, ct);

        return [..visitorRegistrations, ..assignedTickets];
    }
}
