
namespace ConventionSystem.Application.Event.Commands.ApproveVersion;

public sealed record ApproveVersionCommand(
    Guid EventId,
    IReadOnlyList<ApproveOrganizerTicketAssignment>? OrganizerTicketAssignments = null) : ICommand;

public sealed record ApproveOrganizerTicketAssignment(
    Guid PersonId,
    Guid? TicketTypeId);
