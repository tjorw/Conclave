using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.RespondToEventComment;

public sealed class RespondToEventCommentHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<RespondToEventCommentCommand>
{
    protected override async Task ExecuteAsync(RespondToEventCommentCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCommentsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);

        if (!context.Convention.IsAdministrator(performedById)
            && !context.Edition.IsCategoryResponsible(ev.CategoryId, performedById))
        {
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att hantera kommentarer för detta evenemang.");
        }

        ev.RespondToComment(new EventCommentId(command.CommentId), performedById, command.Response);
        await eventRepository.SaveAsync(ct);
    }
}
