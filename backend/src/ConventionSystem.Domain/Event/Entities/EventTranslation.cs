using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Event.Entities;

public sealed class EventTranslation
{
    public EventId EventId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private EventTranslation() { }

    internal EventTranslation(EventId eventId, string locale, string title, string description)
    {
        EventId = eventId;
        Locale = locale;
        Title = title;
        Description = description;
    }

    internal void Update(string title, string description)
    {
        Title = title;
        Description = description;
    }
}
