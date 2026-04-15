using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.SubmitForReview;

public sealed class SubmitForReviewHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<SubmitForReviewCommand>
{
    public async Task Handle(SubmitForReviewCommand command, CancellationToken ct)
    {
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithCoOrganisersAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        if (!ev.IsOrganiser(performedById))
            throw new UnauthorizedAccessException("Utföraren har inte behörighet att skicka in detta evenemang för granskning.");

        if (!currentUser.IsAdmin)
        {
            var edition = await editionRepository.GetByIdAsync(ev.EditionId, ct)
                ?? throw new InvalidOperationException($"Upplagan för evenemanget hittades inte.");
            if (!edition.OrganiserRegistrationOpen)
                throw new InvalidOperationException("Arrangemangsansökan är inte öppen.");
        }

        ev.SubmitForReview();
        await eventRepository.SaveAsync(ct);
    }
}
