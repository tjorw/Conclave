using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionConfigureLocalesTests
{
    [Fact]
    public void ConfigureLocales_ValidLocales_SetsLocales()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());

        edition.ConfigureLocales(["sv", "en"], "sv", personId);

        Assert.Equal(2, edition.Locales.Count);
        Assert.Single(edition.Locales, l => l.Locale == "sv" && l.IsPrimary);
        Assert.Single(edition.Locales, l => l.Locale == "en" && !l.IsPrimary);
    }

    [Fact]
    public void ConfigureLocales_SingleLocale_SetsPrimary()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());

        edition.ConfigureLocales(["sv"], "sv", personId);

        Assert.Single(edition.Locales);
        Assert.True(edition.Locales[0].IsPrimary);
    }

    [Fact]
    public void ConfigureLocales_UnsupportedLocale_ThrowsArgumentException()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            edition.ConfigureLocales(["sv", "xx"], "sv", personId));
    }

    [Fact]
    public void ConfigureLocales_PrimaryNotInLocales_ThrowsArgumentException()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            edition.ConfigureLocales(["sv"], "en", personId));
    }

    [Fact]
    public void ConfigureLocales_EmptyList_ThrowsArgumentException()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            edition.ConfigureLocales([], "sv", personId));
    }

    [Fact]
    public void ConfigureLocales_ReplacesExistingLocales()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());
        edition.ConfigureLocales(["sv", "en"], "sv", personId);

        edition.ConfigureLocales(["sv"], "sv", personId);

        Assert.Single(edition.Locales);
        Assert.Equal("sv", edition.Locales[0].Locale);
    }

    [Fact]
    public void ConfigureLocales_RaisesDomainEvent()
    {
        var edition = CreateEdition();
        var personId = new PersonId(Guid.NewGuid());

        edition.ConfigureLocales(["sv"], "sv", personId);

        Assert.NotEmpty(edition.DomainEvents);
    }

    private static Domain.Convention.Aggregates.Edition CreateEdition()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        return convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
    }
}
