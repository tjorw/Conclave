using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Entities;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;

namespace ConventionSystem.Domain.Tests.Event;

public sealed class SessionTeamAssignmentTests
{
    private static readonly DateTime BaseTime = new(2027, 3, 1, 10, 0, 0);
    private static readonly TimeSlot DefaultSlot = new(BaseTime, BaseTime.AddHours(2));

    private static Domain.Event.Aggregates.Event CreateEvent()
        => new(EventId.New(), EditionId.New(), CategoryId.New(), PersonId.New());

    private static Session AddSession(Domain.Event.Aggregates.Event ev)
    {
        var session = ev.CreateSession(VenueId.New(), DefaultSlot, 10, Domain.Event.Enums.StartType.FixedTime);
        ev.ClearDomainEvents();
        return session;
    }

    [Fact]
    public void AssignTeam_OnActiveSession_ReturnsAssignment()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);
        var registrationId = Guid.NewGuid();
        var assignedById = PersonId.New();

        var assignment = session.AssignTeam(registrationId, assignedById);

        Assert.Equal(session.Id, assignment.SessionId);
        Assert.Equal(registrationId, assignment.TeamEventRegistrationId);
        Assert.Equal(assignedById, assignment.AssignedByPersonId);
    }

    [Fact]
    public void AssignTeam_OnActiveSession_AddsToCollection()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);

        session.AssignTeam(Guid.NewGuid(), PersonId.New());

        Assert.Single(session.TeamAssignments);
    }

    [Fact]
    public void AssignTeam_OnInactiveSession_Throws()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);
        ev.DeactivateSession(session.Id, PersonId.New());

        Assert.Throws<SessionInactiveCannotEditException>(() =>
            session.AssignTeam(Guid.NewGuid(), PersonId.New()));
    }

    [Fact]
    public void AssignTeam_SameRegistrationTwice_Throws()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);
        var registrationId = Guid.NewGuid();
        session.AssignTeam(registrationId, PersonId.New());

        Assert.Throws<TeamAlreadyAssignedToSessionException>(() =>
            session.AssignTeam(registrationId, PersonId.New()));
    }

    [Fact]
    public void RemoveTeamAssignment_WhenExists_RemovesFromCollection()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);
        var registrationId = Guid.NewGuid();
        session.AssignTeam(registrationId, PersonId.New());

        session.RemoveTeamAssignment(registrationId);

        Assert.Empty(session.TeamAssignments);
    }

    [Fact]
    public void RemoveTeamAssignment_WhenNotFound_Throws()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);

        Assert.Throws<TeamAssignmentNotFoundException>(() =>
            session.RemoveTeamAssignment(Guid.NewGuid()));
    }

    [Fact]
    public void Event_AssignTeamToSession_RaisesTeamAssignedToSessionEvent()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);
        var registrationId = Guid.NewGuid();
        var assignedById = PersonId.New();

        ev.AssignTeamToSession(session.Id, registrationId, assignedById);

        var evt = ev.DomainEvents.OfType<TeamAssignedToSession>().Single();
        Assert.Equal(ev.Id, evt.EventId);
        Assert.Equal(session.Id, evt.SessionId);
        Assert.Equal(registrationId, evt.TeamEventRegistrationId);
        Assert.Equal(assignedById, evt.AssignedByPersonId);
    }

    [Fact]
    public void Event_AssignTeamToSession_WhenSessionNotFound_Throws()
    {
        var ev = CreateEvent();

        Assert.Throws<SessionNotFoundException>(() =>
            ev.AssignTeamToSession(SessionId.New(), Guid.NewGuid(), PersonId.New()));
    }

    [Fact]
    public void Event_RemoveTeamFromSession_RaisesTeamRemovedFromSessionEvent()
    {
        var ev = CreateEvent();
        var session = AddSession(ev);
        var registrationId = Guid.NewGuid();
        session.AssignTeam(registrationId, PersonId.New());
        ev.ClearDomainEvents();

        ev.RemoveTeamFromSession(session.Id, registrationId, PersonId.New());

        Assert.Single(ev.DomainEvents.OfType<TeamRemovedFromSession>());
    }

    [Fact]
    public void Event_RemoveTeamFromSession_WhenSessionNotFound_Throws()
    {
        var ev = CreateEvent();

        Assert.Throws<SessionNotFoundException>(() =>
            ev.RemoveTeamFromSession(SessionId.New(), Guid.NewGuid(), PersonId.New()));
    }
}
