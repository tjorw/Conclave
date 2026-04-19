using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Registration.Queries.GetMyAssignedShifts;

public sealed record GetMyAssignedShiftsQuery(Guid EditionId) : IQuery<IReadOnlyList<MyAssignedShiftSummaryDto>>;
