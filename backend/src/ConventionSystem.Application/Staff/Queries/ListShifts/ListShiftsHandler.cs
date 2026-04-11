using ConventionSystem.Application.Common;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Staff.Queries.ListShifts;

public sealed class ListShiftsHandler(IShiftRepository shiftRepository)
    : IQueryHandler<ListShiftsQuery, IReadOnlyList<ShiftSummaryDto>>
{
    public Task<IReadOnlyList<ShiftSummaryDto>> Handle(ListShiftsQuery query, CancellationToken ct)
        => shiftRepository.ListByStationIdAsync(new StationId(query.StationId), ct);
}
