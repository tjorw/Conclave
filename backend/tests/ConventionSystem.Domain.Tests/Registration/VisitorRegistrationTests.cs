using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public class VisitorRegistrationTests
{
    private static VisitorRegistration CreateRegistration()
        => new(VisitorRegistrationId.New(), PersonId.New(), EditionId.New(), TicketId.New());

    [Fact]
    public void Constructor_SetsStatusPendingPayment()
    {
        var registration = CreateRegistration();

        Assert.Equal(VisitorRegistrationStatus.PendingPayment, registration.Status);
    }

    [Fact]
    public void ConfirmPayment_TransitionsToConfirmed_RaisesEvent()
    {
        var registration = CreateRegistration();

        registration.ConfirmPayment("EXT-REF-123");

        Assert.Equal(VisitorRegistrationStatus.Confirmed, registration.Status);
        Assert.Equal("EXT-REF-123", registration.PaymentReference);
        Assert.Single(registration.DomainEvents.OfType<VisitorRegistrationConfirmed>());
    }

    [Fact]
    public void ConfirmPayment_AlreadyConfirmed_Throws()
    {
        var registration = CreateRegistration();
        registration.ConfirmPayment("REF-1");

        Assert.Throws<VisitorRegistrationPaymentStateInvalidException>(() => registration.ConfirmPayment("REF-2"));
    }

    [Fact]
    public void Cancel_TransitionsToCancelled()
    {
        var registration = CreateRegistration();

        registration.Cancel();

        Assert.Equal(VisitorRegistrationStatus.Cancelled, registration.Status);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var registration = CreateRegistration();
        registration.Cancel();

        Assert.Throws<VisitorRegistrationAlreadyCancelledException>(() => registration.Cancel());
    }
}
