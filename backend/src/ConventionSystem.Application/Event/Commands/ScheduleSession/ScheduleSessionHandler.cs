using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;

namespace ConventionSystem.Application.Event.Commands.ScheduleSession;

public sealed class ScheduleSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : ICommandHandler<ScheduleSessionCommand, Guid>
{
    public async Task<Guid> Handle(ScheduleSessionCommand command, CancellationToken ct)
    {
        var venueId = new VenueId(command.VenueId);
        var performedById = currentUser.PersonId;

        var ev = await eventRepository.GetByIdWithSessionsAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var context = await EditionContextLoader.LoadWithCategoriesAndVenuesAsync(
            editionRepository,
            conventionRepository,
            ev.EditionId,
            ct);

        ApplicationAuthorization.EnsureConventionAdmin(context.Convention, performedById, "Endast administratörer kan schemalägga pass.");  
        if (!context.Edition.Venues.Any(v => v.Id == venueId))
            throw new InvalidOperationException("Lokalen hittades inte på denna upplaga.");

        var timeSlot = new TimeSlot(command.StartTime, command.EndTime);
        var session = ev.CreateSession(venueId, timeSlot, command.MaxSeats, command.StartType);

        await eventRepository.SaveAsync(ct);
        return session.Id.Value;
    }
}
