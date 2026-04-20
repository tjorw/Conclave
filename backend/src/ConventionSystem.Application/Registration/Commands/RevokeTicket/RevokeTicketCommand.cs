
namespace ConventionSystem.Application.Registration.Commands.RevokeTicket;

public sealed record RevokeTicketCommand(
    Guid TicketId) : ICommand;
