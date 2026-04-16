using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyAssignedShifts;

public sealed class GetMyAssignedShiftsHandler(
    IMyScheduleRepository repository,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyAssignedShiftsQuery, IReadOnlyList<MyAssignedShiftSummaryDto>>
{
    public Task<IReadOnlyList<MyAssignedShiftSummaryDto>> Handle(GetMyAssignedShiftsQuery query, CancellationToken ct)
        => repository.ListMyAssignedShiftsAsync(currentUser.PersonId, new EditionId(query.EditionId), ct);
}
