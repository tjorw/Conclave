using MediatR;

namespace ConventionSystem.Application.Registration.Queries.GetMyAssignedShifts;

public sealed record GetMyAssignedShiftsQuery(Guid EditionId) : IRequest<IReadOnlyList<MyAssignedShiftSummaryDto>>;
