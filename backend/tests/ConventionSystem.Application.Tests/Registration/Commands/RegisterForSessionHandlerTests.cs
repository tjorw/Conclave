using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Queries;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RegisterForSession;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RegisterForSessionHandlerTests
{
    private readonly ISessionRegistrationRepository _sessionRegRepo = Substitute.For<ISessionRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IRegistrationRuleService _ruleService = Substitute.For<IRegistrationRuleService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RegisterForSessionHandler _handler;

    public RegisterForSessionHandlerTests()
    {
        _handler = new RegisterForSessionHandler(
            _sessionRegRepo, _ticketRepo, _eventRepo, _ruleService, _currentUser);
    }

    private Ticket SetupPaidTicket(int maxSeats = 10, AllocationMode mode = AllocationMode.DirectConfirmation)
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        ticket.ConfirmPayment();
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _ruleService.ValidateTicket(Arg.Any<TicketId>(), Arg.Any<SessionId>()).Returns(true);
        _currentUser.PersonId.Returns(ticket.PersonId);
        _eventRepo.GetSessionAllocationInfoAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(new SessionAllocationInfoDto(mode, maxSeats));
        _sessionRegRepo.CountConfirmedBySessionIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(0);
        return ticket;
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegistrationId()
    {
        var ticket = SetupPaidTicket();

        var id = await _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var ticket = SetupPaidTicket();

        await _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default);

        await _sessionRegRepo.Received(1).AddAndSaveAsync(Arg.Any<SessionRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DirectConfirmation_FullSession_Throws()
    {
        var ticket = SetupPaidTicket(maxSeats: 5, mode: AllocationMode.DirectConfirmation);
        _sessionRegRepo.CountConfirmedBySessionIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(5);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_Queue_FullSession_RegistersAsPending()
    {
        var ticket = SetupPaidTicket(maxSeats: 5, mode: AllocationMode.Queue);
        _sessionRegRepo.CountConfirmedBySessionIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(5);

        await _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default);

        await _sessionRegRepo.Received(1).AddAndSaveAsync(
            Arg.Is<SessionRegistration>(r => r.Status == SessionRegistrationStatus.Pending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TicketNotPaid_Throws()
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketBelongsToDifferentPerson_Throws()
    {
        var ticket = SetupPaidTicket();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketNotFound_Throws()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_DuplicateRegistration_Throws()
    {
        var ticket = SetupPaidTicket();
        var sessionId = Guid.NewGuid();
        _sessionRegRepo.HasRegistrationAsync(Arg.Any<PersonId>(), Arg.Any<SessionId>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(sessionId, ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketInvalidForSession_Throws()
    {
        var ticket = SetupPaidTicket();
        _ruleService.ValidateTicket(Arg.Any<TicketId>(), Arg.Any<SessionId>()).Returns(false);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), ticket.Id.Value), default));
    }
}
