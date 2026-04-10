using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.EditEventDraft;

public sealed class EditEventDraftHandler(IEventRepository eventRepository)
    : IRequestHandler<EditEventDraftCommand>
{
    public async Task Handle(EditEventDraftCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithDraftVersionAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var draft = ev.GetDraftVersion();
        draft.EditTitle(command.Title);
        draft.EditDescription(command.Description);
        draft.SetRegistrationType(command.RegistrationType, command.DropInRules);

        await eventRepository.SaveAsync(ct);
    }
}
