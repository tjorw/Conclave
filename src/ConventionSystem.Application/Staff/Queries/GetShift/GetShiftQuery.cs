using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Staff.Queries.GetShift;

public sealed record GetShiftQuery(Guid ShiftId) : IQuery<ShiftDto?>;
