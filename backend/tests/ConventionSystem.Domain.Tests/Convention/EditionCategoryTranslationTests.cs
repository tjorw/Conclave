using ConventionSystem.Domain.Convention.Exceptions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionCategoryTranslationTests
{
    private static (Domain.Convention.Aggregates.Edition edition,
                    Domain.Convention.Entities.Category category) CreateEditionWithCategory()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "ansvarig@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Test 2027", period, staff.Id, evt.Id);
        var category = edition.CreateCategory("Brädspel", responsible.Id);
        return (edition, category);
    }

    [Fact]
    public void SetCategoryTranslation_ValidInput_AddsTranslation()
    {
        var (edition, category) = CreateEditionWithCategory();

        edition.SetCategoryTranslation(category.Id, "en", "Board Games");

        Assert.Single(category.Translations);
        Assert.Equal("en", category.Translations[0].Locale);
        Assert.Equal("Board Games", category.Translations[0].Name);
    }

    [Fact]
    public void SetCategoryTranslation_CalledTwiceForSameLocale_UpdatesTranslation()
    {
        var (edition, category) = CreateEditionWithCategory();
        edition.SetCategoryTranslation(category.Id, "en", "Board Games");

        edition.SetCategoryTranslation(category.Id, "en", "Board Games Updated");

        Assert.Single(category.Translations);
        Assert.Equal("Board Games Updated", category.Translations[0].Name);
    }

    [Fact]
    public void SetCategoryTranslation_DifferentLocales_AddsBoth()
    {
        var (edition, category) = CreateEditionWithCategory();

        edition.SetCategoryTranslation(category.Id, "sv", "Brädspel Sv");
        edition.SetCategoryTranslation(category.Id, "en", "Board Games");

        Assert.Equal(2, category.Translations.Count);
    }

    [Fact]
    public void SetCategoryTranslation_UnsupportedLocale_ThrowsArgumentException()
    {
        var (edition, category) = CreateEditionWithCategory();

        Assert.Throws<ArgumentException>(() =>
            edition.SetCategoryTranslation(category.Id, "xx", "Unknown"));
    }

    [Fact]
    public void SetCategoryTranslation_UnknownCategory_ThrowsCategoryNotFoundInEditionException()
    {
        var (edition, _) = CreateEditionWithCategory();

        Assert.Throws<CategoryNotFoundInEditionException>(() =>
            edition.SetCategoryTranslation(CategoryId.New(), "en", "Board Games"));
    }

    [Fact]
    public void SetCategoryTranslation_EmptyName_ThrowsArgumentException()
    {
        var (edition, category) = CreateEditionWithCategory();

        Assert.Throws<ArgumentException>(() =>
            edition.SetCategoryTranslation(category.Id, "en", ""));
    }

    [Fact]
    public void SetCategoryTranslation_LocaleIsCaseInsensitive()
    {
        var (edition, category) = CreateEditionWithCategory();
        edition.SetCategoryTranslation(category.Id, "EN", "Board Games");

        edition.SetCategoryTranslation(category.Id, "en", "Board Games v2");

        Assert.Single(category.Translations);
        Assert.Equal("Board Games v2", category.Translations[0].Name);
    }

    [Fact]
    public void SetCategoryTranslation_LocaleStoredInLowerCase()
    {
        var (edition, category) = CreateEditionWithCategory();

        edition.SetCategoryTranslation(category.Id, "EN", "Board Games");

        Assert.Equal("en", category.Translations[0].Locale);
    }

    [Fact]
    public void SetCategoryTranslation_TrimsWhitespace()
    {
        var (edition, category) = CreateEditionWithCategory();

        edition.SetCategoryTranslation(category.Id, "en", "  Board Games  ");

        Assert.Equal("Board Games", category.Translations[0].Name);
    }
}
