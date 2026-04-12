using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.EditEventDraft;

public sealed class EditEventDraftHandler(IEventRepository eventRepository)
    : IRequestHandler<EditEventDraftCommand>
{
    public async Task Handle(EditEventDraftCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        ev.EditTitle(command.Title);
        ev.EditDescription(command.Description);
        ev.SetRegistrationType(command.RegistrationType, command.DropInRules);

        await eventRepository.SaveAsync(ct);
    }
}
