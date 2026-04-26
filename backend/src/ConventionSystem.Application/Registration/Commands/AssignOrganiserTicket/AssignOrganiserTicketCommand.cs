namespace ConventionSystem.Application.Registration.Commands.AssignOrganiserTicket;

public sealed record AssignOrganiserTicketCommand(
    Guid EditionId,
    Guid PersonId,
    Guid? TicketTypeId) : ICommand;
