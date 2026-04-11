using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.DomainEventHandlers;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.DomainEventHandlers;

public class SessionDeactivatedHandlerTests
{
    private readonly ISessionRegistrationRepository _repo = Substitute.For<ISessionRegistrationRepository>();
    private readonly SessionDeactivatedHandler _handler;

    public SessionDeactivatedHandlerTests()
    {
        _handler = new SessionDeactivatedHandler(_repo);
    }

    private static SessionRegistration CreateConfirmedRegistration()
        => new(SessionRegistrationId.New(), SessionId.New(), PersonId.New(), TicketId.New());

    [Fact]
    public async Task Handle_ConfirmedRegistrations_CancelsAll()
    {
        var sessionId = SessionId.New();
        var reg1 = CreateConfirmedRegistration();
        var reg2 = CreateConfirmedRegistration();
        _repo.GetAllConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration> { reg1, reg2 });

        await _handler.Handle(new SessionDeactivated(sessionId, EventId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        Assert.Equal(SessionRegistrationStatus.Cancelled, reg1.Status);
        Assert.Equal(SessionRegistrationStatus.Cancelled, reg2.Status);
    }

    [Fact]
    public async Task Handle_ConfirmedRegistrations_CallsSave()
    {
        var sessionId = SessionId.New();
        _repo.GetAllConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration> { CreateConfirmedRegistration() });

        await _handler.Handle(new SessionDeactivated(sessionId, EventId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        await _repo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRegistrations_DoesNotCallSave()
    {
        var sessionId = SessionId.New();
        _repo.GetAllConfirmedBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionRegistration>());

        await _handler.Handle(new SessionDeactivated(sessionId, EventId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        await _repo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
