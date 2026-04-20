using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.DomainEventHandlers;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Events;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.DomainEventHandlers;

public class TicketRevokedHandlerTests
{
    private readonly ISessionRegistrationRepository _repo = Substitute.For<ISessionRegistrationRepository>();
    private readonly TicketRevokedHandler _handler;

    public TicketRevokedHandlerTests()
    {
        _handler = new TicketRevokedHandler(_repo);
    }

    private static SessionRegistration CreateConfirmedRegistration(TicketId ticketId)
        => new(SessionRegistrationId.New(), SessionId.New(), PersonId.New(), ticketId);

    [Fact]
    public async Task Handle_ConfirmedRegistrations_CancelsAll()
    {
        var ticketId = TicketId.New();
        var reg1 = CreateConfirmedRegistration(ticketId);
        var reg2 = CreateConfirmedRegistration(ticketId);

        _repo.GetAllConfirmedByTicketIdAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns([reg1, reg2]);

        await _handler.Handle(new TicketRevoked(ticketId, PersonId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        Assert.Equal(SessionRegistrationStatus.Cancelled, reg1.Status);
        Assert.Equal(SessionRegistrationStatus.Cancelled, reg2.Status);
    }

    [Fact]
    public async Task Handle_ConfirmedRegistrations_CallsSave()
    {
        var ticketId = TicketId.New();
        _repo.GetAllConfirmedByTicketIdAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns([CreateConfirmedRegistration(ticketId)]);

        await _handler.Handle(new TicketRevoked(ticketId, PersonId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        await _repo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRegistrations_DoesNotCallSave()
    {
        var ticketId = TicketId.New();
        _repo.GetAllConfirmedByTicketIdAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(new TicketRevoked(ticketId, PersonId.New(), PersonId.New(), DateTimeOffset.UtcNow), default);

        await _repo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}