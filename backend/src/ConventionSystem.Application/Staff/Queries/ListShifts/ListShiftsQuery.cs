using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Staff.Queries.ListShifts;

public sealed record ListShiftsQuery(Guid StationId) : IQuery<IReadOnlyList<ShiftSummaryDto>>;
