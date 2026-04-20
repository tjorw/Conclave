
namespace ConventionSystem.Application.Event.Commands.AddEventComment;

public sealed record AddEventCommentCommand(
    Guid EventId,
    string Comment) : ICommand;
