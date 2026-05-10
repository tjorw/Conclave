using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Domain.Tests.Content;

public class PageTranslationTests
{
    [Fact]
    public void SetTranslation_NewLocale_AddsTranslation()
    {
        var page = CreatePage();

        page.SetTranslation("en", "English Title", "English content.");

        Assert.Single(page.Translations);
        Assert.Equal("en", page.Translations[0].Locale);
        Assert.Equal("English Title", page.Translations[0].Title);
    }

    [Fact]
    public void SetTranslation_ExistingLocale_UpdatesTranslation()
    {
        var page = CreatePage();
        page.SetTranslation("en", "Old Title", "Old content.");

        page.SetTranslation("en", "New Title", "New content.");

        Assert.Single(page.Translations);
        Assert.Equal("New Title", page.Translations[0].Title);
        Assert.Equal("New content.", page.Translations[0].Content);
    }

    [Fact]
    public void SetTranslation_MultipleLocales_AddsDistinct()
    {
        var page = CreatePage();

        page.SetTranslation("sv", "Svensk titel", "Svenskt innehåll.");
        page.SetTranslation("en", "English Title", "English content.");

        Assert.Equal(2, page.Translations.Count);
    }

    [Fact]
    public void SetTranslation_UnsupportedLocale_ThrowsArgumentException()
    {
        var page = CreatePage();

        Assert.Throws<ArgumentException>(() =>
            page.SetTranslation("xx", "Title", "Content"));
    }

    [Fact]
    public void SetTranslation_TitleTooLong_ThrowsArgumentException()
    {
        var page = CreatePage();
        var longTitle = new string('X', 301);

        Assert.Throws<ArgumentException>(() =>
            page.SetTranslation("en", longTitle, "Content"));
    }

    private static Page CreatePage()
    {
        var conventionId = ConventionId.New();
        return new Page(PageId.New(), conventionId, null, "test-slug", "Titel", "Innehåll");
    }
}
