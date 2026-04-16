using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.AddSessionRequest;

public sealed class AddSessionRequestHandler(IEventRepository eventRepository)
    : IRequestHandler<AddSessionRequestCommand, Guid>
{
    public async Task<Guid> Handle(AddSessionRequestCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdWithSessionRequestsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var request = ev.AddSessionRequest(
            command.Description, command.DurationMinutes, command.Seats, command.StartType);

        await eventRepository.SaveAsync(ct);
        return request.Id.Value;
    }
}
