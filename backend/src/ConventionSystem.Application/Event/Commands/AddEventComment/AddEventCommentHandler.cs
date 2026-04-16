using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.AddEventComment;

public sealed class AddEventCommentHandler(
    IEventRepository eventRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AddEventCommentCommand>
{
    public async Task Handle(AddEventCommentCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        if (!ev.IsOrganiser(performedById))
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att kommentera detta evenemang.");

        ev.AddOrganiserComment(performedById, command.Comment);
        await eventRepository.SaveAsync(ct);
    }
}
