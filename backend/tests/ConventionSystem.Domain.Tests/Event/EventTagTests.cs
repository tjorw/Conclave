using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Aggregates;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Tests.Event;

public sealed class EventTagTests
{
    private static ConventionSystem.Domain.Event.Aggregates.Event CreateDraftEvent()
        => new(
            EventId.New(),
            EditionId.New(),
            CategoryId.New(),
            PersonId.New());

    [Fact]
    public void SetProgramTags_RemovesCaseInsensitiveDuplicates()
    {
        var ev = CreateDraftEvent();

        ev.SetProgramTags(["Barnvänligt", "barnvänligt", "Nybörjare"]);

        Assert.Equal(["Barnvänligt", "Nybörjare"], ev.ProgramTags.Select(t => t.Name));
    }

    [Fact]
    public void SetProgramTags_ThrowsOnEmptyTagName()
    {
        var ev = CreateDraftEvent();

        Assert.Throws<ArgumentException>(() => ev.SetProgramTags(["  "]));
    }

    [Fact]
    public void SetProgramTags_ThrowsOnTooLongTagName()
    {
        var ev = CreateDraftEvent();
        var tooLongTag = new string('a', 65);

        Assert.Throws<ArgumentException>(() => ev.SetProgramTags([tooLongTag]));
    }
}
