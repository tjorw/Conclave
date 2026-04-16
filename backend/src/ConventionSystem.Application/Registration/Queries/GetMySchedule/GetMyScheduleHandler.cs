using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Registration.Queries.GetMySchedule;

public sealed class GetMyScheduleHandler(
    IMyScheduleRepository repository,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyScheduleQuery, IReadOnlyList<MyScheduleItemDto>>
{
    public Task<IReadOnlyList<MyScheduleItemDto>> Handle(GetMyScheduleQuery query, CancellationToken ct)
        => repository.GetMyScheduleAsync(currentUser.PersonId, new EditionId(query.EditionId), ct);
}
