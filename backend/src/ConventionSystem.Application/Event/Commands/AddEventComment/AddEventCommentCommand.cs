using MediatR;

namespace ConventionSystem.Application.Event.Commands.AddEventComment;

public sealed record AddEventCommentCommand(
    Guid EventId,
    string Comment) : IRequest;
