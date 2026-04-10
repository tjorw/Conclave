using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CollectTicket;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CollectTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CollectTicketHandler _handler;

    public CollectTicketHandlerTests()
    {
        _handler = new CollectTicketHandler(_ticketRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_CollectsTicket()
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        ticket.ConfirmPayment();
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);

        await _handler.Handle(new CollectTicketCommand(ticket.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.TicketStatus.Collected, ticket.Status);
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TicketNotFound_Throws()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CollectTicketCommand(Guid.NewGuid()), default));
    }
}
