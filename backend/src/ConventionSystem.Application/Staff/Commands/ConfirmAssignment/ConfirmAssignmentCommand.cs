
namespace ConventionSystem.Application.Staff.Commands.ConfirmAssignment;

public sealed record ConfirmAssignmentCommand(
    Guid ShiftId,
    Guid AssignmentId) : ICommand;
