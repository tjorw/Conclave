using ConventionSystem.Domain.Convention;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;

namespace ConventionSystem.Domain.Tests.Convention;

public class EditionSetContentTests
{
    [Fact]
    public void SetContent_NewKey_AddsEntry()
    {
        var edition = CreateEdition();

        edition.SetContent(EditionContentKey.HeroTitle, "Välkommen!");

        Assert.Single(edition.Content);
        Assert.Equal(EditionContentKey.HeroTitle, edition.Content[0].Key);
        Assert.Equal("Välkommen!", edition.Content[0].Value);
    }

    [Fact]
    public void SetContent_ExistingKey_UpdatesValue()
    {
        var edition = CreateEdition();
        edition.SetContent(EditionContentKey.HeroTitle, "Gammalt värde");

        edition.SetContent(EditionContentKey.HeroTitle, "Nytt värde");

        Assert.Single(edition.Content);
        Assert.Equal("Nytt värde", edition.Content[0].Value);
    }

    [Fact]
    public void SetContent_TrimsWhitespace()
    {
        var edition = CreateEdition();

        edition.SetContent(EditionContentKey.HeroIngress, "  text med mellanslag  ");

        Assert.Equal("text med mellanslag", edition.Content[0].Value);
    }

    [Fact]
    public void SetContent_MultipleKeys_AddsDistinctEntries()
    {
        var edition = CreateEdition();

        edition.SetContent(EditionContentKey.HeroTitle, "Titel");
        edition.SetContent(EditionContentKey.HeroIngress, "Ingress");
        edition.SetContent(EditionContentKey.CtaVisitorLabel, "Bli besökare");
        edition.SetContent(EditionContentKey.FeaturedSectionTitle, "Utvalda evenemang");

        Assert.Equal(4, edition.Content.Count);
    }

    [Fact]
    public void SetContent_EmptyValue_Stored()
    {
        var edition = CreateEdition();
        edition.SetContent(EditionContentKey.HeroTitle, "Befintligt");

        edition.SetContent(EditionContentKey.HeroTitle, "");

        Assert.Equal(string.Empty, edition.Content[0].Value);
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
