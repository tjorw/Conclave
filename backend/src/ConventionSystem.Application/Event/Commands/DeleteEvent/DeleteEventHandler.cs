using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.DeleteEvent;

public sealed class DeleteEventHandler(
    IEventRepository eventRepository,
    ICurrentUser currentUser)
    : CommandHandler<DeleteEventCommand>
{
    protected override async Task ExecuteAsync(DeleteEventCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        if (!currentUser.IsAdmin && !ev.IsOrganiser(currentUser.PersonId))
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att ta bort detta evenemang.");

        var allowedStatuses = currentUser.IsAdmin
            ? new[] { EventStatus.Draft, EventStatus.Cancelled }
            : new[] { EventStatus.Draft };

        if (!allowedStatuses.Contains(ev.Status))
            throw new EventCannotBeDeletedException();

        await eventRepository.DeleteAsync(ev, ct);
    }
}
