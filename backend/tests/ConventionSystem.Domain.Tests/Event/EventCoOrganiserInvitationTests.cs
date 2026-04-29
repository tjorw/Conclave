using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Domain.Tests.Event;

public class EventCoOrganiserInvitationTests
{
    private static Domain.Event.Aggregates.Event CreateEvent() =>
        new(EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());

    private static Domain.Event.Aggregates.Event CreateEventWithLimit(int limit)
    {
        var ev = CreateEvent();
        ev.AdjustCoOrganiserLimit(limit);
        return ev;
    }

    [Fact]
    public void SetCoOrganiserCount_ValidValue_UpdatesCount()
    {
        var ev = CreateEvent();
        ev.SetCoOrganiserCount(3);
        Assert.Equal(3, ev.CoOrganiserCount);
    }

    [Fact]
    public void SetCoOrganiserCount_Zero_Allowed()
    {
        var ev = CreateEvent();
        ev.SetCoOrganiserCount(0);
        Assert.Equal(0, ev.CoOrganiserCount);
    }

    [Fact]
    public void SetCoOrganiserCount_Negative_ThrowsArgumentException()
    {
        var ev = CreateEvent();
        Assert.Throws<ArgumentException>(() => ev.SetCoOrganiserCount(-1));
    }

    [Fact]
    public void AdjustCoOrganiserLimit_ValidValue_UpdatesLimit()
    {
        var ev = CreateEvent();
        ev.AdjustCoOrganiserLimit(5);
        Assert.Equal(5, ev.CoOrganiserLimit);
    }

    [Fact]
    public void AdjustCoOrganiserLimit_Negative_ThrowsArgumentException()
    {
        var ev = CreateEvent();
        Assert.Throws<ArgumentException>(() => ev.AdjustCoOrganiserLimit(-1));
    }

    [Fact]
    public void CreateInvitation_WithinLimit_AddsInvitation()
    {
        var ev = CreateEventWithLimit(2);
        var createdBy = PersonId.New();

        var invitation = ev.CreateInvitation("test@example.com", createdBy);

        Assert.Single(ev.CoOrganiserInvitations);
        Assert.Equal("test@example.com", invitation.Email);
        Assert.Equal(createdBy, invitation.CreatedById);
    }

    [Fact]
    public void CreateInvitation_GeneratesUniqueCode()
    {
        var ev = CreateEventWithLimit(5);

        var inv1 = ev.CreateInvitation("a@example.com", PersonId.New());
        var inv2 = ev.CreateInvitation("b@example.com", PersonId.New());

        Assert.NotEqual(inv1.Code, inv2.Code);
    }

    [Fact]
    public void CreateInvitation_LimitZero_ThrowsCoOrganiserLimitExceededException()
    {
        var ev = CreateEvent();

        Assert.Throws<CoOrganiserLimitExceededException>(() =>
            ev.CreateInvitation("test@example.com", PersonId.New()));
    }

    [Fact]
    public void CreateInvitation_ActiveInvitationsAtLimit_ThrowsCoOrganiserLimitExceededException()
    {
        var ev = CreateEventWithLimit(1);
        ev.CreateInvitation("first@example.com", PersonId.New());

        Assert.Throws<CoOrganiserLimitExceededException>(() =>
            ev.CreateInvitation("second@example.com", PersonId.New()));
    }

    [Fact]
    public void CreateInvitation_DuplicateActiveEmail_ThrowsCoOrganiserAlreadyInvitedException()
    {
        var ev = CreateEventWithLimit(5);
        ev.CreateInvitation("test@example.com", PersonId.New());

        Assert.Throws<CoOrganiserAlreadyInvitedException>(() =>
            ev.CreateInvitation("TEST@EXAMPLE.COM", PersonId.New()));
    }

    [Fact]
    public void CreateInvitation_CancelledEmailCanBeReinvited()
    {
        var ev = CreateEventWithLimit(5);
        var first = ev.CreateInvitation("test@example.com", PersonId.New());
        ev.CancelInvitation(first.Id, PersonId.New());

        var second = ev.CreateInvitation("test@example.com", PersonId.New());

        Assert.Equal(2, ev.CoOrganiserInvitations.Count);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void CreateInvitation_EventCancelled_ThrowsEventIsCancelledAndReadOnlyException()
    {
        var ev = CreateEventWithLimit(3);
        ev.CancelEvent(PersonId.New());

        Assert.Throws<EventIsCancelledAndReadOnlyException>(() =>
            ev.CreateInvitation("test@example.com", PersonId.New()));
    }

    [Fact]
    public void CancelInvitation_ActiveInvitation_SetsStatusCancelled()
    {
        var ev = CreateEventWithLimit(3);
        var invitation = ev.CreateInvitation("test@example.com", PersonId.New());
        var cancelledBy = PersonId.New();

        ev.CancelInvitation(invitation.Id, cancelledBy);

        Assert.Equal(Domain.Event.Enums.CoOrganiserInvitationStatus.Cancelled, invitation.Status);
        Assert.Equal(cancelledBy, invitation.CancelledById);
    }

    [Fact]
    public void CancelInvitation_NotFound_ThrowsCoOrganiserInvitationNotFoundException()
    {
        var ev = CreateEventWithLimit(3);

        Assert.Throws<CoOrganiserInvitationNotFoundException>(() =>
            ev.CancelInvitation(CoOrganiserInvitationId.New(), PersonId.New()));
    }

    [Fact]
    public void CancelInvitation_AlreadyCancelled_ThrowsCoOrganiserInvitationNotActiveException()
    {
        var ev = CreateEventWithLimit(3);
        var invitation = ev.CreateInvitation("test@example.com", PersonId.New());
        ev.CancelInvitation(invitation.Id, PersonId.New());

        Assert.Throws<CoOrganiserInvitationNotActiveException>(() =>
            ev.CancelInvitation(invitation.Id, PersonId.New()));
    }

    [Fact]
    public void RedeemInvitation_ValidCodeAndEmail_AddsCoOrganiser()
    {
        var ev = CreateEventWithLimit(3);
        var redeemerEmail = "redeemer@example.com";
        var invitation = ev.CreateInvitation(redeemerEmail, PersonId.New());
        var redeemerId = PersonId.New();

        var coOrganiser = ev.RedeemInvitation(invitation.Code, redeemerEmail, redeemerId);

        Assert.Equal(redeemerId, coOrganiser.PersonId);
        Assert.Single(ev.CoOrganisers);
        Assert.Equal(Domain.Event.Enums.CoOrganiserInvitationStatus.Redeemed, invitation.Status);
    }

    [Fact]
    public void RedeemInvitation_InvalidCode_ThrowsInvalidInvitationCodeException()
    {
        var ev = CreateEventWithLimit(3);

        Assert.Throws<InvalidInvitationCodeException>(() =>
            ev.RedeemInvitation("nonexistent-code", "test@example.com", PersonId.New()));
    }

    [Fact]
    public void RedeemInvitation_WrongEmail_ThrowsCoOrganiserInvitationEmailMismatchException()
    {
        var ev = CreateEventWithLimit(3);
        var invitation = ev.CreateInvitation("correct@example.com", PersonId.New());

        Assert.Throws<CoOrganiserInvitationEmailMismatchException>(() =>
            ev.RedeemInvitation(invitation.Code, "wrong@example.com", PersonId.New()));
    }

    [Fact]
    public void RedeemInvitation_AlreadyRedeemed_ThrowsCoOrganiserInvitationNotActiveException()
    {
        var ev = CreateEventWithLimit(3);
        var email = "test@example.com";
        var invitation = ev.CreateInvitation(email, PersonId.New());
        ev.RedeemInvitation(invitation.Code, email, PersonId.New());

        Assert.Throws<CoOrganiserInvitationNotActiveException>(() =>
            ev.RedeemInvitation(invitation.Code, email, PersonId.New()));
    }

    [Fact]
    public void RedeemInvitation_LeadOrganiser_ThrowsLeadOrganiserCannotBeCoOrganiserException()
    {
        var leadId = PersonId.New();
        var ev = new Domain.Event.Aggregates.Event(EventId.New(), EditionId.New(), CategoryId.New(), leadId);
        ev.AdjustCoOrganiserLimit(3);
        var invitation = ev.CreateInvitation("lead@example.com", PersonId.New());

        Assert.Throws<LeadOrganiserCannotBeCoOrganiserException>(() =>
            ev.RedeemInvitation(invitation.Code, "lead@example.com", leadId));
    }
}
