using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Tests.Registration;

public class TicketTests
{
    private static Ticket CreateTicket()
        => new(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());

    [Fact]
    public void Constructor_SetsStatusReserved()
    {
        var ticket = CreateTicket();

        Assert.Equal(TicketStatus.Reserved, ticket.Status);
    }

    [Fact]
    public void ConfirmPayment_TransitionsToPaid()
    {
        var ticket = CreateTicket();

        ticket.ConfirmPayment();

        Assert.Equal(TicketStatus.Paid, ticket.Status);
    }

    [Fact]
    public void ConfirmPayment_NotReserved_Throws()
    {
        var ticket = CreateTicket();
        ticket.ConfirmPayment();

        Assert.Throws<InvalidOperationException>(() => ticket.ConfirmPayment());
    }

    [Fact]
    public void Collect_TransitionsToCollected_RaisesEvent()
    {
        var ticket = CreateTicket();
        ticket.ConfirmPayment();
        var performedById = PersonId.New();

        ticket.Collect(performedById);

        Assert.Equal(TicketStatus.Collected, ticket.Status);
        Assert.Equal(performedById, ticket.CollectedById);
        Assert.Single(ticket.DomainEvents.OfType<TicketCollected>());
    }

    [Fact]
    public void Collect_NotPaid_Throws()
    {
        var ticket = CreateTicket();

        Assert.Throws<InvalidOperationException>(() => ticket.Collect(PersonId.New()));
    }

    [Fact]
    public void Revoke_TransitionsToRevoked_RaisesEvent()
    {
        var ticket = CreateTicket();

        ticket.Revoke(PersonId.New());

        Assert.Equal(TicketStatus.Revoked, ticket.Status);
        Assert.Single(ticket.DomainEvents.OfType<TicketRevoked>());
    }

    [Fact]
    public void Revoke_AlreadyRevoked_Throws()
    {
        var ticket = CreateTicket();
        ticket.Revoke(PersonId.New());

        Assert.Throws<InvalidOperationException>(() => ticket.Revoke(PersonId.New()));
    }
}
