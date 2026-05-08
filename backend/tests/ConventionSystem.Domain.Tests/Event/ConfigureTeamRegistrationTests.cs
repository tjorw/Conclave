using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Aggregates;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Tests.Event;

public sealed class ConfigureTeamRegistrationTests
{
    private static Domain.Event.Aggregates.Event CreateEvent()
    {
        var id = EventId.New();
        var editionId = EditionId.New();
        var categoryId = CategoryId.New();
        var organiserId = PersonId.New();
        return new Domain.Event.Aggregates.Event(id, editionId, categoryId, organiserId);
    }

    [Fact]
    public void Default_RegistrationMode_IsIndividual()
    {
        var ev = CreateEvent();

        Assert.Equal(RegistrationMode.Individual, ev.RegistrationMode);
        Assert.Null(ev.TeamSize);
    }

    [Fact]
    public void ConfigureTeamRegistration_Team_SetsTeamSize()
    {
        var ev = CreateEvent();

        ev.ConfigureTeamRegistration(RegistrationMode.Team, 2, 6);

        Assert.Equal(RegistrationMode.Team, ev.RegistrationMode);
        Assert.NotNull(ev.TeamSize);
        Assert.Equal(2, ev.TeamSize!.Min);
        Assert.Equal(6, ev.TeamSize!.Max);
    }

    [Fact]
    public void ConfigureTeamRegistration_Individual_ClearsTeamSize()
    {
        var ev = CreateEvent();
        ev.ConfigureTeamRegistration(RegistrationMode.Team, 2, 6);

        ev.ConfigureTeamRegistration(RegistrationMode.Individual, null, null);

        Assert.Equal(RegistrationMode.Individual, ev.RegistrationMode);
        Assert.Null(ev.TeamSize);
    }

    [Fact]
    public void ConfigureTeamRegistration_Team_WithoutSizes_Throws()
    {
        var ev = CreateEvent();

        Assert.Throws<ArgumentException>(() =>
            ev.ConfigureTeamRegistration(RegistrationMode.Team, null, null));
    }

    [Fact]
    public void ConfigureTeamRegistration_MinLessThanOne_Throws()
    {
        var ev = CreateEvent();

        Assert.Throws<ArgumentException>(() =>
            ev.ConfigureTeamRegistration(RegistrationMode.Team, 0, 4));
    }

    [Fact]
    public void ConfigureTeamRegistration_MaxLessThanMin_Throws()
    {
        var ev = CreateEvent();

        Assert.Throws<ArgumentException>(() =>
            ev.ConfigureTeamRegistration(RegistrationMode.Team, 4, 2));
    }

    [Fact]
    public void ConfigureTeamRegistration_CancelledEvent_Throws()
    {
        var ev = CreateEvent();
        ev.CancelEvent(PersonId.New());

        Assert.Throws<Domain.Event.Exceptions.EventIsCancelledAndReadOnlyException>(() =>
            ev.ConfigureTeamRegistration(RegistrationMode.Team, 2, 6));
    }
}
