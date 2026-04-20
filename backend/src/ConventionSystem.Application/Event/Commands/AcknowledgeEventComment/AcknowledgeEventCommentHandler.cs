using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.AcknowledgeEventComment;

public sealed class AcknowledgeEventCommentHandler(
    IEventRepository eventRepository,
    ICurrentUser currentUser)
    : CommandHandler<AcknowledgeEventCommentCommand>
{
    protected override async Task ExecuteAsync(AcknowledgeEventCommentCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCommentsAndCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        if (!ev.IsOrganiser(performedById))
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att kvittera kommentarer för detta evenemang.");

        ev.AcknowledgeComment(new EventCommentId(command.CommentId), performedById);
        await eventRepository.SaveAsync(ct);
    }
}
