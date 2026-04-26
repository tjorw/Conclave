using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMyVisitorRegistration;

public sealed class GetMyVisitorRegistrationHandler(
    ITicketRepository ticketRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyVisitorRegistrationQuery, IReadOnlyList<MyVisitorRegistrationDto>>
{
    public Task<IReadOnlyList<MyVisitorRegistrationDto>> Handle(GetMyVisitorRegistrationQuery query, CancellationToken ct)
        => ticketRepository.ListByPersonAndEditionAsync(
            currentUser.PersonId, new EditionId(query.EditionId), ct);
}
