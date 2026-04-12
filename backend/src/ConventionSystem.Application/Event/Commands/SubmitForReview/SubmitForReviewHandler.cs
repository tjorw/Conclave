using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.SubmitForReview;

public sealed class SubmitForReviewHandler(IEventRepository eventRepository)
    : IRequestHandler<SubmitForReviewCommand>
{
    public async Task Handle(SubmitForReviewCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        ev.SubmitForReview();
        await eventRepository.SaveAsync(ct);
    }
}
