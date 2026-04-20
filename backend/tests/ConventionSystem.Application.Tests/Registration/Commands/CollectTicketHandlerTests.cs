using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CollectTicket;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CollectTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CollectTicketHandler _handler;

    public CollectTicketHandlerTests()
    {
        _handler = new CollectTicketHandler(_ticketRepo, _ticketTypeRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_CollectsTicketAndReturnsPerks()
    {
        var ticketTypeId = TicketTypeId.New();
        var ticket = new Ticket(TicketId.New(), ticketTypeId, PersonId.New(), EditionId.New());
        ticket.ConfirmPayment();
        var ticketType = new TicketType(ticketTypeId, ticket.EditionId, "Helgbiljett", 15000, TicketTypeCategory.Visitor);
        ticketType.AddPerk("T-shirt");
        ticketType.AddPerk("Matkupong dag 1");

        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>()).Returns(ticketType);

        var result = await _handler.Handle(new CollectTicketCommand(ticket.Id.Value), default);

        Assert.Equal(TicketStatus.Collected, ticket.Status);
        Assert.Equal(ticket.Id.Value, result.TicketId);
        Assert.Equal(["T-shirt", "Matkupong dag 1"], result.Perks);
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TicketNotFound_Throws()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new CollectTicketCommand(Guid.NewGuid()), default));
    }
}
