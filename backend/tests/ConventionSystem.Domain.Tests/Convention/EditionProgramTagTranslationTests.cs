using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionProgramTagTranslationTests
{
    private static Domain.Convention.Aggregates.Edition CreateEditionWithTag(out string tagName)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
        tagName = "Familjevänligt";
        edition.AddProgramTagDefinition(tagName);
        return edition;
    }

    [Fact]
    public void SetProgramTagTranslation_ValidInput_AddsTranslation()
    {
        var edition = CreateEditionWithTag(out var tagName);

        edition.SetProgramTagTranslation(tagName, "en", "Family Friendly");

        Assert.Single(edition.ProgramTagTranslations);
        Assert.Equal(tagName, edition.ProgramTagTranslations[0].TagName);
        Assert.Equal("en", edition.ProgramTagTranslations[0].Locale);
        Assert.Equal("Family Friendly", edition.ProgramTagTranslations[0].TranslatedName);
    }

    [Fact]
    public void SetProgramTagTranslation_CalledTwiceForSameLocale_UpdatesTranslation()
    {
        var edition = CreateEditionWithTag(out var tagName);
        edition.SetProgramTagTranslation(tagName, "en", "Family Friendly");

        edition.SetProgramTagTranslation(tagName, "en", "Family Friendly v2");

        Assert.Single(edition.ProgramTagTranslations);
        Assert.Equal("Family Friendly v2", edition.ProgramTagTranslations[0].TranslatedName);
    }

    [Fact]
    public void SetProgramTagTranslation_DifferentLocales_AddsBoth()
    {
        var edition = CreateEditionWithTag(out var tagName);

        edition.SetProgramTagTranslation(tagName, "sv", "Familjevänligt Sv");
        edition.SetProgramTagTranslation(tagName, "en", "Family Friendly");

        Assert.Equal(2, edition.ProgramTagTranslations.Count);
    }

    [Fact]
    public void SetProgramTagTranslation_UnsupportedLocale_ThrowsArgumentException()
    {
        var edition = CreateEditionWithTag(out var tagName);

        Assert.Throws<ArgumentException>(() =>
            edition.SetProgramTagTranslation(tagName, "xx", "Unknown"));
    }

    [Fact]
    public void SetProgramTagTranslation_UnknownTag_ThrowsProgramTagDefinitionNotFoundException()
    {
        var edition = CreateEditionWithTag(out _);

        Assert.Throws<ProgramTagDefinitionNotFoundException>(() =>
            edition.SetProgramTagTranslation("NonExistentTag", "en", "Test"));
    }

    [Fact]
    public void SetProgramTagTranslation_EmptyTranslation_ThrowsArgumentException()
    {
        var edition = CreateEditionWithTag(out var tagName);

        Assert.Throws<ArgumentException>(() =>
            edition.SetProgramTagTranslation(tagName, "en", ""));
    }

    [Fact]
    public void SetProgramTagTranslation_TooLongTranslation_ThrowsArgumentException()
    {
        var edition = CreateEditionWithTag(out var tagName);
        var tooLong = new string('a', 65);

        Assert.Throws<ArgumentException>(() =>
            edition.SetProgramTagTranslation(tagName, "en", tooLong));
    }

    [Fact]
    public void SetProgramTagTranslation_LocaleIsCaseInsensitive()
    {
        var edition = CreateEditionWithTag(out var tagName);
        edition.SetProgramTagTranslation(tagName, "EN", "Family Friendly");

        edition.SetProgramTagTranslation(tagName, "en", "Family Friendly v2");

        Assert.Single(edition.ProgramTagTranslations);
        Assert.Equal("Family Friendly v2", edition.ProgramTagTranslations[0].TranslatedName);
    }

    [Fact]
    public void SetProgramTagTranslation_LocaleStoredInLowerCase()
    {
        var edition = CreateEditionWithTag(out var tagName);

        edition.SetProgramTagTranslation(tagName, "EN", "Family Friendly");

        Assert.Equal("en", edition.ProgramTagTranslations[0].Locale);
    }

    [Fact]
    public void SetProgramTagTranslation_TrimsWhitespace()
    {
        var edition = CreateEditionWithTag(out var tagName);

        edition.SetProgramTagTranslation(tagName, "en", "  Family Friendly  ");

        Assert.Equal("Family Friendly", edition.ProgramTagTranslations[0].TranslatedName);
    }

    [Fact]
    public void SetProgramTagTranslation_TagNameLookupIsCaseInsensitive()
    {
        var edition = CreateEditionWithTag(out var tagName);

        edition.SetProgramTagTranslation(tagName.ToUpperInvariant(), "en", "Family Friendly");

        Assert.Single(edition.ProgramTagTranslations);
    }
}
