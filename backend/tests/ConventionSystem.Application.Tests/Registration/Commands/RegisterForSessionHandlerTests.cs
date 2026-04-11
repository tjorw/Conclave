using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RegisterForSession;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Registration.Services;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RegisterForSessionHandlerTests
{
    private readonly ISessionRegistrationRepository _sessionRegRepo = Substitute.For<ISessionRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly IRegistrationRuleService _ruleService = Substitute.For<IRegistrationRuleService>();
    private readonly RegisterForSessionHandler _handler;

    public RegisterForSessionHandlerTests()
    {
        _handler = new RegisterForSessionHandler(_sessionRegRepo, _ticketRepo, _ruleService);
    }

    private Ticket SetupPaidTicket()
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        ticket.ConfirmPayment();
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _ruleService.ValidateSeatAvailability(Arg.Any<SessionId>()).Returns(true);
        _ruleService.ValidateTicket(Arg.Any<TicketId>(), Arg.Any<SessionId>()).Returns(true);
        return ticket;
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegistrationId()
    {
        var ticket = SetupPaidTicket();

        var id = await _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), ticket.PersonId.Value, ticket.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var ticket = SetupPaidTicket();

        await _handler.Handle(
            new RegisterForSessionCommand(Guid.NewGuid(), ticket.PersonId.Value, ticket.Id.Value), default);

        await _sessionRegRepo.Received(1).AddAndSaveAsync(Arg.Any<SessionRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TicketNotPaid_Throws()
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), ticket.PersonId.Value, ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketBelongsToDifferentPerson_Throws()
    {
        var ticket = SetupPaidTicket();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), Guid.NewGuid(), ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NoSeatsAvailable_Throws()
    {
        var ticket = SetupPaidTicket();
        _ruleService.ValidateSeatAvailability(Arg.Any<SessionId>()).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), ticket.PersonId.Value, ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketNotFound_Throws()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(
                new RegisterForSessionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
