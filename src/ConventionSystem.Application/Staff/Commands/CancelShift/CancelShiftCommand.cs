using MediatR;

namespace ConventionSystem.Application.Staff.Commands.CancelShift;

public sealed record CancelShiftCommand(
    Guid ShiftId,
    Guid PerformedById) : IRequest;
