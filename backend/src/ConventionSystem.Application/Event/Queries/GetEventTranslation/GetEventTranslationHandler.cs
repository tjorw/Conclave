using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Queries.GetEventTranslation;

public sealed class GetEventTranslationHandler(IEventRepository eventRepository)
    : IQueryHandler<GetEventTranslationQuery, EventTranslationDto?>
{
    public async Task<EventTranslationDto?> Handle(GetEventTranslationQuery query, CancellationToken ct)
    {
        var eventId = new EventId(query.EventId);
        var ev = await eventRepository.GetByIdAsync(eventId, ct)
            ?? throw new ResourceNotFoundException("Evenemang", query.EventId.ToString());

        var translation = await eventRepository.GetTranslationAsync(eventId, query.Locale, ct);
        if (translation is null) return null;

        return new EventTranslationDto(query.EventId, translation.Locale, translation.Title, translation.Description);
    }
}
