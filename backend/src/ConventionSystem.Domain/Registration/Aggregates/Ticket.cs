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
    public PromotionCodeRedemptionId? PromotionCodeRedemptionId { get; private set; }
    public int? FinalPrice { get; private set; }
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
        if (Status == TicketStatus.Paid)
            throw new TicketAlreadyPaidException();
        if (Status != TicketStatus.Reserved)
            throw new TicketNotReservedForPaymentException();

        Status = TicketStatus.Paid;
        RaiseDomainEvent(new TicketPaid(Id, PersonId, EditionId, DateTimeOffset.UtcNow));
    }

    public void CancelOwn()
    {
        if (Status != TicketStatus.Reserved)
            throw new TicketNotReservedForCancellationException();

        Status = TicketStatus.Revoked;
        RaiseDomainEvent(new TicketRevoked(Id, PersonId, PersonId, DateTimeOffset.UtcNow));
    }

    public void ApplyPromotion(PromotionCodeRedemptionId redemptionId, int finalPrice)
    {
        if (Status != TicketStatus.Reserved)
            throw new TicketNotReservedForPromotionException();

        if (finalPrice < 0)
            throw new ArgumentException("Slutpris får inte vara negativt.", nameof(finalPrice));

        PromotionCodeRedemptionId = redemptionId;
        FinalPrice = finalPrice;

        if (finalPrice == 0)
            ConfirmPayment();
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
