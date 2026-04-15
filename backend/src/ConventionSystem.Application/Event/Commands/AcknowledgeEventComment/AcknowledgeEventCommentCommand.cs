using MediatR;

namespace ConventionSystem.Application.Event.Commands.AcknowledgeEventComment;

public sealed record AcknowledgeEventCommentCommand(
    Guid EventId,
    Guid CommentId) : IRequest;
