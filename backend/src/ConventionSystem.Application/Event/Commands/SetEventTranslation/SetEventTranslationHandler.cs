using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.SetEventTranslation;

public sealed class SetEventTranslationHandler(
    IEventRepository eventRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<SetEventTranslationCommand>
{
    protected override async Task ExecuteAsync(SetEventTranslationCommand command, CancellationToken ct)
    {
        var eventId = new EventId(command.EventId);
        var ev = await eventRepository.GetByIdWithTranslationsAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var convention = await conventionRepository.GetSingleAsync(ct);
        var isAdmin = convention is not null && convention.IsAdministrator(currentUser.PersonId);

        if (!isAdmin && !ev.IsOrganiser(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är varken administratör eller arrangör för detta evenemang.");

        ev.SetTranslation(command.Locale, command.Title, command.Description);
        await eventRepository.SaveAsync(ct);
    }
}
