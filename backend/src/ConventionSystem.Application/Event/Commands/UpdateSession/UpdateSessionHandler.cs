using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.UpdateSession;

public sealed class UpdateSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateSessionCommand>
{
    public async Task Handle(UpdateSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var venueId = new VenueId(command.VenueId);
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithSessionsAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        var edition = await editionRepository.GetByIdWithCategoriesAndVenuesAsync(ev.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att redigera sessioner för detta evenemang.");

        if (!edition.Venues.Any(v => v.Id == venueId))
            throw new InvalidOperationException("Lokalen hittades inte på denna upplaga.");

        var timeSlot = new TimeSlot(command.StartTime, command.EndTime);
        ev.UpdateSession(sessionId, venueId, timeSlot, command.MaxSeats, command.StartType, performedById);

        await eventRepository.SaveAsync(ct);
    }
}
