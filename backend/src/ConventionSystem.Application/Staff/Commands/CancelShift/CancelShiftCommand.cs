
namespace ConventionSystem.Application.Staff.Commands.CancelShift;

public sealed record CancelShiftCommand(
    Guid ShiftId) : ICommand;
