
namespace ConventionSystem.Application.Staff.Commands.CancelAssignment;

public sealed record CancelAssignmentCommand(
    Guid ShiftId,
    Guid AssignmentId) : ICommand;
