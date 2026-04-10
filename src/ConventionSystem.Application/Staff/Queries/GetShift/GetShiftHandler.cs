using ConventionSystem.Application.Common;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Ids;

namespace ConventionSystem.Application.Staff.Queries.GetShift;

public sealed class GetShiftHandler(IShiftRepository shiftRepository)
    : IQueryHandler<GetShiftQuery, ShiftDto?>
{
    public Task<ShiftDto?> Handle(GetShiftQuery query, CancellationToken ct)
        => shiftRepository.GetProjectedByIdAsync(new ShiftId(query.ShiftId), ct);
}
