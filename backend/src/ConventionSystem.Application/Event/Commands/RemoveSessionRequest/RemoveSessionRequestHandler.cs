using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.RemoveSessionRequest;

public sealed class RemoveSessionRequestHandler(IEventRepository eventRepository)
    : CommandHandler<RemoveSessionRequestCommand>
{
    protected override async Task ExecuteAsync(RemoveSessionRequestCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithSessionRequestsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        ev.RemoveSessionRequest(new SessionRequestId(command.SessionRequestId));
        await eventRepository.SaveAsync(ct);
    }
}
