using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AllocateSessionRegistrations;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AllocateSessionRegistrationsHandlerTests
{
    private readonly ISessionRegistrationRepository _sessionRegRepo = Substitute.For<ISessionRegistrationRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AllocateSessionRegistrationsHandler _handler;

    public AllocateSessionRegistrationsHandlerTests()
    {
        _handler = new AllocateSessionRegistrationsHandler(
            _sessionRegRepo, _eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Event.Aggregates.Event ev,
             SessionId sessionId)
        Setup(int maxSeats = 10, int confirmedCount = 0)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test");
        var admin = convention.RegisterPerson("Admin", "admin@test.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var staffCoord = convention.CreatePerson("S", "s@test.com");
        var eventCoord = convention.CreatePerson("E", "e@test.com");
        var edition = convention.CreateEdition("Ed", period, staffCoord.Id, eventCoord.Id);
        edition.Publish(admin.Id);

        var ev = new Domain.Event.Aggregates.Event(
            new EventId(Guid.NewGuid()),
            edition.Id,
            new CategoryId(Guid.NewGuid()),
            admin.Id);

        var sessionId = new SessionId(Guid.NewGuid());

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _eventRepo.GetSessionAllocationInfoAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new SessionAllocationInfoDto(AllocationMode.Queue, maxSeats));
        _sessionRegRepo.CountConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(confirmedCount);

        return (convention, admin, edition, ev, sessionId);
    }

    private static SessionRegistration MakePending(SessionId sessionId)
        => new(
            SessionRegistrationId.New(),
            sessionId,
            PersonId.New(),
            TicketId.New(),
            SessionRegistrationStatus.Pending);

    [Fact]
    public async Task Handle_FirstComeFirstServed_ConfirmsUpToMaxSeats_CancelsRest()
    {
        var (_, admin, _, ev, sessionId) = Setup(maxSeats: 2, confirmedCount: 0);
        _currentUser.PersonId.Returns(admin.Id);

        var pending = new List<SessionRegistration>
        {
            MakePending(sessionId),
            MakePending(sessionId),
            MakePending(sessionId)
        };
        _sessionRegRepo.GetPendingBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns(pending);

        await _handler.Handle(
            new AllocateSessionRegistrationsCommand(ev.Id.Value, sessionId.Value, "FirstComeFirstServed"),
            default);

        var confirmed = pending.Count(r => r.Status == SessionRegistrationStatus.Confirmed);
        var cancelled = pending.Count(r => r.Status == SessionRegistrationStatus.Cancelled);
        Assert.Equal(2, confirmed);
        Assert.Equal(1, cancelled);
        await _sessionRegRepo.Received(1).SaveAllAsync(
            Arg.Is<IReadOnlyList<SessionRegistration>>(l => l.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPending_DoesNotCallSaveAll()
    {
        var (_, admin, _, ev, sessionId) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        _sessionRegRepo.GetPendingBySessionAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration>());

        await _handler.Handle(
            new AllocateSessionRegistrationsCommand(ev.Id.Value, sessionId.Value, "FirstComeFirstServed"),
            default);

        await _sessionRegRepo.DidNotReceive().SaveAllAsync(Arg.Any<IReadOnlyList<SessionRegistration>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionFull_CancelsAllPending()
    {
        var (_, admin, _, ev, sessionId) = Setup(maxSeats: 2, confirmedCount: 2);
        _currentUser.PersonId.Returns(admin.Id);
        var pending = new List<SessionRegistration> { MakePending(sessionId) };
        _sessionRegRepo.GetPendingBySessionAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(pending);

        await _handler.Handle(
            new AllocateSessionRegistrationsCommand(ev.Id.Value, sessionId.Value, "FirstComeFirstServed"),
            default);

        Assert.Equal(SessionRegistrationStatus.Cancelled, pending[0].Status);
        await _sessionRegRepo.Received(1).SaveAllAsync(
            Arg.Is<IReadOnlyList<SessionRegistration>>(l => l.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnauthorizedPerson_Throws()
    {
        var (convention, _, _, ev, sessionId) = Setup();
        var nonAdmin = convention.CreatePerson("X", "x@test.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);
        _sessionRegRepo.GetPendingBySessionAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns([MakePending(sessionId)]);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new AllocateSessionRegistrationsCommand(ev.Id.Value, sessionId.Value, "FirstComeFirstServed"),
                default));
    }

    [Fact]
    public async Task Handle_InvalidStrategy_Throws()
    {
        var (_, admin, _, ev, sessionId) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(
                new AllocateSessionRegistrationsCommand(ev.Id.Value, sessionId.Value, "Ogiltigt"),
                default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new AllocateSessionRegistrationsCommand(Guid.NewGuid(), Guid.NewGuid(), "FirstComeFirstServed"),
                default));
    }
}
