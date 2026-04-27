namespace ConventionSystem.Application.Registration.Commands.AssignStaffTicket;

public sealed record AssignStaffTicketCommand(
    Guid EditionId,
    Guid PersonId,
    Guid? TicketTypeId) : ICommand;
