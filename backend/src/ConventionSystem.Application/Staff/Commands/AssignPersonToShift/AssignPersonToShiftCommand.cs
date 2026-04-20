
namespace ConventionSystem.Application.Staff.Commands.AssignPersonToShift;

public sealed record AssignPersonToShiftCommand(
    Guid ShiftId,
    Guid PersonId) : ICommand<Guid>;
