using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.AddSessionRequest;

public sealed class AddSessionRequestHandler(IEventRepository eventRepository)
    : IRequestHandler<AddSessionRequestCommand, Guid>
{
    public async Task<Guid> Handle(AddSessionRequestCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithDraftVersionAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var draft = ev.GetDraftVersion();
        var request = draft.AddSessionRequest(
            command.Description, command.DurationMinutes, command.Seats, command.StartType);

        await eventRepository.SaveAsync(ct);
        return request.Id.Value;
    }
}
