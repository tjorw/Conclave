using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AssignTeamToSession;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public sealed class AssignTeamToSessionHandlerTests
{
    private readonly ITeamEventRegistrationRepository _regRepo = Substitute.For<ITeamEventRegistrationRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AssignTeamToSessionHandler _handler;

    public AssignTeamToSessionHandlerTests()
    {
        _handler = new AssignTeamToSessionHandler(
            _regRepo, _eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private record TestSetup(
        Domain.Event.Aggregates.Event Event,
        Domain.Event.Entities.Session Session,
        TeamEventRegistration Registration,
        Domain.Convention.Aggregates.Convention Convention,
        Domain.Convention.Aggregates.Edition Edition,
        Domain.Convention.Entities.Person Admin);

    private TestSetup Setup(TeamRegistrationStatus status = TeamRegistrationStatus.Confirmed)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@test.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@test.com");
        var eventCoord = convention.CreatePerson("Event", "event@test.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, CategoryId.New(), PersonId.New());
        var slot = new TimeSlot(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 12, 0, 0));
        var session = ev.CreateSession(VenueId.New(), slot, 10, StartType.FixedTime);

        var registration = new TeamEventRegistration(
            TeamEventRegistrationId.New(), TeamId.New(), ev.Id, edition.Id);

        if (status == TeamRegistrationStatus.Confirmed)
            registration.Confirm();

        _currentUser.PersonId.Returns(admin.Id);
        _regRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _eventRepo.GetByIdWithSessionsAndTeamAssignmentsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return new TestSetup(ev, session, registration, convention, edition, admin);
    }

    [Fact]
    public async Task Handle_AdminAssigns_AddsTeamAssignment()
    {
        var s = Setup();

        await _handler.Handle(new AssignTeamToSessionCommand(
            s.Event.Id.Value, s.Session.Id.Value, s.Registration.Id.Value), default);

        Assert.Single(s.Session.TeamAssignments);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _regRepo.GetByIdAsync(Arg.Any<TeamEventRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((TeamEventRegistration?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new AssignTeamToSessionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_RegistrationNotConfirmed_Throws()
    {
        var s = Setup(TeamRegistrationStatus.Pending);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(new AssignTeamToSessionCommand(
                s.Event.Id.Value, s.Session.Id.Value, s.Registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_RegistrationEventMismatch_Throws()
    {
        var s = Setup();
        var otherEventId = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(new AssignTeamToSessionCommand(
                otherEventId, s.Session.Id.Value, s.Registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        var s = Setup();
        _eventRepo.GetByIdWithSessionsAndTeamAssignmentsAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new AssignTeamToSessionCommand(
                s.Event.Id.Value, s.Session.Id.Value, s.Registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var s = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new AssignTeamToSessionCommand(
                s.Event.Id.Value, s.Session.Id.Value, s.Registration.Id.Value), default));
    }
}
