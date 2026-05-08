using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public sealed class TeamEventRegistrationTests
{
    private static TeamEventRegistration CreateRegistration()
        => new(TeamEventRegistrationId.New(), TeamId.New(), EventId.New(), EditionId.New());

    [Fact]
    public void Constructor_SetsStatusToPending()
    {
        var registration = CreateRegistration();

        Assert.Equal(TeamRegistrationStatus.Pending, registration.Status);
    }

    [Fact]
    public void Constructor_RaisesTeamEventRegistrationCreatedEvent()
    {
        var registration = CreateRegistration();

        Assert.Single(registration.DomainEvents.OfType<TeamEventRegistrationCreated>());
    }

    [Fact]
    public void Confirm_FromPending_SetsStatusToConfirmed()
    {
        var registration = CreateRegistration();
        registration.ClearDomainEvents();

        registration.Confirm();

        Assert.Equal(TeamRegistrationStatus.Confirmed, registration.Status);
    }

    [Fact]
    public void Confirm_RaisesConfirmedEvent()
    {
        var registration = CreateRegistration();
        registration.ClearDomainEvents();

        registration.Confirm();

        Assert.Single(registration.DomainEvents.OfType<TeamEventRegistrationConfirmed>());
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_Throws()
    {
        var registration = CreateRegistration();
        registration.Confirm();
        registration.ClearDomainEvents();

        Assert.Throws<TeamRegistrationNotPendingException>(() => registration.Confirm());
    }

    [Fact]
    public void Confirm_WhenCancelled_Throws()
    {
        var registration = CreateRegistration();
        registration.Cancel(PersonId.New());
        registration.ClearDomainEvents();

        Assert.Throws<TeamRegistrationNotPendingException>(() => registration.Confirm());
    }

    [Fact]
    public void Cancel_FromPending_SetsStatusToCancelled()
    {
        var registration = CreateRegistration();
        registration.ClearDomainEvents();

        registration.Cancel(PersonId.New());

        Assert.Equal(TeamRegistrationStatus.Cancelled, registration.Status);
    }

    [Fact]
    public void Cancel_FromConfirmed_SetsStatusToCancelled()
    {
        var registration = CreateRegistration();
        registration.Confirm();
        registration.ClearDomainEvents();

        registration.Cancel(PersonId.New());

        Assert.Equal(TeamRegistrationStatus.Cancelled, registration.Status);
    }

    [Fact]
    public void Cancel_RaisesCancelledEventWithCorrectPersonId()
    {
        var registration = CreateRegistration();
        var cancelledBy = PersonId.New();
        registration.ClearDomainEvents();

        registration.Cancel(cancelledBy);

        var evt = registration.DomainEvents.OfType<TeamEventRegistrationCancelled>().Single();
        Assert.Equal(cancelledBy, evt.CancelledByPersonId);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var registration = CreateRegistration();
        registration.Cancel(PersonId.New());
        registration.ClearDomainEvents();

        Assert.Throws<TeamRegistrationAlreadyCancelledException>(() => registration.Cancel(PersonId.New()));
    }
}
