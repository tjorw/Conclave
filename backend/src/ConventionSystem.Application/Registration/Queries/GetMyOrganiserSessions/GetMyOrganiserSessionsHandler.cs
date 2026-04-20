using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMyOrganiserSessions;

public sealed class GetMyOrganiserSessionsHandler(
    IMyScheduleRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyOrganiserSessionsQuery, IReadOnlyList<MyOrganiserSessionSummaryDto>>
{
    public Task<IReadOnlyList<MyOrganiserSessionSummaryDto>> Handle(GetMyOrganiserSessionsQuery query, CancellationToken ct)
        => repository.ListMyOrganiserSessionsAsync(currentUser.PersonId, new EditionId(query.EditionId), ct);
}
