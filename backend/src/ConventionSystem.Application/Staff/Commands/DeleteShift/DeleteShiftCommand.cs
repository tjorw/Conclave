namespace ConventionSystem.Application.Staff.Commands.DeleteShift;

public sealed record DeleteShiftCommand(Guid ShiftId) : ICommand;
