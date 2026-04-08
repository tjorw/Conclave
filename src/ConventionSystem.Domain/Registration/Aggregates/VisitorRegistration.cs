using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Domain.Registration.Aggregates;

public sealed class VisitorRegistration : AggregateRoot
{
    public VisitorRegistrationId Id { get; private set; }
    public PersonId PersonId { get; private set; }
    public EditionId EditionId { get; private set; }
    public VisitorRegistrationStatus Status { get; private set; }
    public string? PaymentReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private VisitorRegistration() { }

    public VisitorRegistration(VisitorRegistrationId id, PersonId personId, EditionId editionId)
    {
        Id = id;
        PersonId = personId;
        EditionId = editionId;
        Status = VisitorRegistrationStatus.PendingPayment;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmPayment(string externalReferenceId)
    {
        if (Status != VisitorRegistrationStatus.PendingPayment)
            throw new InvalidOperationException("Betalning kan bara bekräftas när registreringen väntar på betalning.");

        PaymentReference = externalReferenceId;
        Status = VisitorRegistrationStatus.Confirmed;
        RaiseDomainEvent(new VisitorRegistrationConfirmed(Id, PersonId, EditionId, DateTimeOffset.UtcNow));
    }

    public void Cancel()
    {
        if (Status == VisitorRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Registreringen är redan avbokad.");

        Status = VisitorRegistrationStatus.Cancelled;
    }
}
