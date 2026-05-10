using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using DomainEvent = ConventionSystem.Domain.Event.Aggregates.Event;

namespace ConventionSystem.Domain.Tests.Event;

public class EventTranslationTests
{
    [Fact]
    public void SetTranslation_NewLocale_AddsTranslation()
    {
        DomainEvent ev = CreateEvent();

        ev.SetTranslation("en", "English Title", "English description.");

        Assert.Single(ev.Translations);
        Assert.Equal("en", ev.Translations[0].Locale);
        Assert.Equal("English Title", ev.Translations[0].Title);
    }

    [Fact]
    public void SetTranslation_ExistingLocale_UpdatesTranslation()
    {
        DomainEvent ev = CreateEvent();
        ev.SetTranslation("en", "Old Title", "Old description.");

        ev.SetTranslation("en", "New Title", "New description.");

        Assert.Single(ev.Translations);
        Assert.Equal("New Title", ev.Translations[0].Title);
        Assert.Equal("New description.", ev.Translations[0].Description);
    }

    [Fact]
    public void SetTranslation_UnsupportedLocale_ThrowsArgumentException()
    {
        DomainEvent ev = CreateEvent();

        Assert.Throws<ArgumentException>(() =>
            ev.SetTranslation("xx", "Title", "Description"));
    }

    [Fact]
    public void SetTranslation_EmptyTitle_ThrowsArgumentException()
    {
        DomainEvent ev = CreateEvent();

        Assert.Throws<ArgumentException>(() =>
            ev.SetTranslation("en", "", "Description"));
    }

    [Fact]
    public void SetTranslation_DescriptionTooLong_ThrowsArgumentException()
    {
        DomainEvent ev = CreateEvent();
        var longDesc = new string('X', 10_001);

        Assert.Throws<ArgumentException>(() =>
            ev.SetTranslation("en", "Title", longDesc));
    }

    [Fact]
    public void SetTranslation_MultipleLocales_AddsDistinct()
    {
        DomainEvent ev = CreateEvent();

        ev.SetTranslation("sv", "Svensk titel", "Svensk beskrivning.");
        ev.SetTranslation("en", "English Title", "English description.");

        Assert.Equal(2, ev.Translations.Count);
    }

    private static DomainEvent CreateEvent()
    {
        var editionId = new EditionId(Guid.NewGuid());
        var categoryId = new CategoryId(Guid.NewGuid());
        var personId = new PersonId(Guid.NewGuid());
        return new DomainEvent(EventId.New(), editionId, categoryId, personId);
    }
}
