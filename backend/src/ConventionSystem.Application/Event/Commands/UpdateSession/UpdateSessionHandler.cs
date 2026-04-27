using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;

namespace ConventionSystem.Application.Event.Commands.UpdateSession;

public sealed class UpdateSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<UpdateSessionCommand>
{
    protected override async Task ExecuteAsync(UpdateSessionCommand command, CancellationToken ct)
    {
        var sessionId = new SessionId(command.SessionId);
        var venueId = new VenueId(command.VenueId);
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithSessionsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAndVenuesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);
        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan uppdatera pass.");    
        if (!context.Edition.Venues.Any(v => v.Id == venueId))
            throw new InvalidOperationException("Lokalen hittades inte på denna upplaga.");

        var timeSlot = new TimeSlot(command.StartTime, command.EndTime);
        ev.UpdateSession(sessionId, venueId, timeSlot, command.MaxSeats, command.StartType, performedById);

        await eventRepository.SaveAsync(ct);
    }
}
