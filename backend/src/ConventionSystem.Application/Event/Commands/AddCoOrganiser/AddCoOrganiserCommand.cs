
namespace ConventionSystem.Application.Event.Commands.AddCoOrganiser;

public sealed record AddCoOrganiserCommand(
    Guid EventId,
    string Email,
    string? Name,
    string? Message,
    Guid ConventionId) : ICommand;
