using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class Ticket : AggregateRoot
{
    public TicketId Id { get; private set; }
    public TicketTypeId TicketTypeId { get; private set; }
    public PersonId PersonId { get; private set; }
    public EditionId EditionId { get; private set; }
    public PersonId? AssignedById { get; private set; }
    public TicketStatus Status { get; private set; }
    public PersonId? CollectedById { get; private set; }
    public DateTimeOffset? CollectedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Ticket() { }

    public Ticket(TicketId id, TicketTypeId ticketTypeId, PersonId personId, EditionId editionId, PersonId? assignedById = null)
    {
        Id = id;
        TicketTypeId = ticketTypeId;
        PersonId = personId;
        EditionId = editionId;
        AssignedById = assignedById;
        Status = TicketStatus.Reserved;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmPayment()
    {
        if (Status != TicketStatus.Reserved)
            throw new TicketNotReservedForPaymentException();

        Status = TicketStatus.Paid;
    }

    public void Collect(PersonId performedById)
    {
        if (Status != TicketStatus.Paid)
            throw new TicketNotPaidForCollectionException();

        Status = TicketStatus.Collected;
        CollectedById = performedById;
        CollectedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TicketCollected(Id, PersonId, performedById, DateTimeOffset.UtcNow));
    }

    public void Revoke(PersonId performedById)
    {
        if (Status == TicketStatus.Revoked)
            throw new TicketAlreadyRevokedException();

        Status = TicketStatus.Revoked;
        RaiseDomainEvent(new TicketRevoked(Id, PersonId, performedById, DateTimeOffset.UtcNow));
    }
}
