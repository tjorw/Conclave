
namespace ConventionSystem.Application.Event.Commands.AddCoOrganiser;

public sealed record AddCoOrganiserCommand(
    Guid EventId,
    Guid PersonId,
    Guid ConventionId) : ICommand;
