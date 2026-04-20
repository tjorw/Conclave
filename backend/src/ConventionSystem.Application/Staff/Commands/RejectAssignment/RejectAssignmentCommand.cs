
namespace ConventionSystem.Application.Staff.Commands.RejectAssignment;

public sealed record RejectAssignmentCommand(
    Guid ShiftId,
    Guid AssignmentId) : ICommand;
