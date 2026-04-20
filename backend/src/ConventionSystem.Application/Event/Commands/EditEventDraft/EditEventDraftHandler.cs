using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.EditEventDraft;

public sealed class EditEventDraftHandler(IEventRepository eventRepository)
    : CommandHandler<EditEventDraftCommand>
{
    protected override async Task ExecuteAsync(EditEventDraftCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        ev.EditTitle(command.Title);
        ev.EditDescription(command.Description);
        ev.SetRegistrationType(command.RegistrationType, command.DropInRules);

        await eventRepository.SaveAsync(ct);
    }
}
