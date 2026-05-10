using ConventionSystem.Application.Common;

namespace ConventionSystem.Application.Event.Queries.GetEventTranslation;

public sealed record GetEventTranslationQuery(Guid EventId, string Locale) : IQuery<EventTranslationDto?>;

public sealed record EventTranslationDto(Guid EventId, string Locale, string Title, string Description);
