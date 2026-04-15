using MediatR;

namespace ConventionSystem.Application.Event.Commands.RespondToEventComment;

public sealed record RespondToEventCommentCommand(
    Guid EventId,
    Guid CommentId,
    string Response) : IRequest;
