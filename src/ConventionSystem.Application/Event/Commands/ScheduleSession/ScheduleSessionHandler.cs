using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.ScheduleSession;

public sealed class ScheduleSessionHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository)
    : IRequestHandler<ScheduleSessionCommand, Guid>
{
    public async Task<Guid> Handle(ScheduleSessionCommand command, CancellationToken ct)
    {
        var venueId = new VenueId(command.VenueId);
        var performedById = new PersonId(command.PerformedById);

        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException($"Evenemanget '{command.EventId}' hittades inte.");

        if (ev.Status != Domain.Event.Enums.EventStatus.Published)
            throw new InvalidOperationException("Evenemanget måste vara publicerat för att sessioner ska kunna schemaläggas.");

        var edition = await editionRepository.GetByIdWithCategoriesAndVenuesAsync(ev.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(performedById)
            && !edition.IsCategoryResponsible(ev.CategoryId, performedById))
            throw new InvalidOperationException("Utföraren har inte behörighet att schemalägga sessioner för detta evenemang.");

        if (!edition.Venues.Any(v => v.Id == venueId))
            throw new InvalidOperationException("Lokalen hittades inte på denna upplaga.");

        var timeSlot = new TimeSlot(command.StartTime, command.EndTime);
        var session = ev.CreateSession(venueId, timeSlot, command.MaxSeats, command.StartType);

        await eventRepository.SaveAsync(ct);
        return session.Id.Value;
    }
}
