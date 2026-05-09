using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public class SessionRegistrationTests
{
    private static SessionRegistration CreateConfirmed()
        => new(
            SessionRegistrationId.New(),
            new SessionId(Guid.NewGuid()),
            PersonId.New(),
            TicketId.New(),
            SessionRegistrationStatus.Confirmed);

    private static SessionRegistration CreatePending()
        => new(
            SessionRegistrationId.New(),
            new SessionId(Guid.NewGuid()),
            PersonId.New(),
            TicketId.New(),
            SessionRegistrationStatus.Pending);

    [Fact]
    public void Constructor_DefaultStatus_IsConfirmed()
    {
        var reg = new SessionRegistration(
            SessionRegistrationId.New(),
            new SessionId(Guid.NewGuid()),
            PersonId.New(),
            TicketId.New());

        Assert.Equal(SessionRegistrationStatus.Confirmed, reg.Status);
    }

    [Fact]
    public void Constructor_PendingStatus_RaisesQueuedEvent()
    {
        var reg = CreatePending();

        var ev = reg.DomainEvents.OfType<SessionRegistrationQueued>().SingleOrDefault();
        Assert.NotNull(ev);
    }

    [Fact]
    public void Constructor_ConfirmedStatus_DoesNotRaiseQueuedEvent()
    {
        var reg = CreateConfirmed();

        Assert.Empty(reg.DomainEvents.OfType<SessionRegistrationQueued>());
    }

    [Fact]
    public void Confirm_TransitionsToConfirmed_RaisesEvent()
    {
        var reg = CreatePending();
        reg.ClearDomainEvents();

        reg.Confirm();

        Assert.Equal(SessionRegistrationStatus.Confirmed, reg.Status);
        Assert.Single(reg.DomainEvents.OfType<SessionRegistrationConfirmed>());
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_Throws()
    {
        var reg = CreateConfirmed();

        Assert.Throws<SessionRegistrationCannotBeConfirmedException>(() => reg.Confirm());
    }

    [Fact]
    public void Cancel_WhenConfirmed_TransitionsToCancelled()
    {
        var reg = CreateConfirmed();

        reg.Cancel();

        Assert.Equal(SessionRegistrationStatus.Cancelled, reg.Status);
    }

    [Fact]
    public void Cancel_WhenPending_TransitionsToCancelled()
    {
        var reg = CreatePending();

        reg.Cancel();

        Assert.Equal(SessionRegistrationStatus.Cancelled, reg.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var reg = CreateConfirmed();
        reg.Cancel();

        Assert.Throws<SessionRegistrationAlreadyCancelledException>(() => reg.Cancel());
    }
}
