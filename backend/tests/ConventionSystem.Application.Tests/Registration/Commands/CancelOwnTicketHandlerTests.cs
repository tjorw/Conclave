using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CancelOwnTicket;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CancelOwnTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelOwnTicketHandler _handler;

    public CancelOwnTicketHandlerTests()
    {
        _handler = new CancelOwnTicketHandler(_ticketRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_OwnerCancelsReservedTicket_RevokesTicket()
    {
        var personId = PersonId.New();
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), personId, EditionId.New());
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _currentUser.PersonId.Returns(personId);

        await _handler.Handle(new CancelOwnTicketCommand(ticket.Id.Value), default);

        Assert.Equal(TicketStatus.Revoked, ticket.Status);
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsForbidden()
    {
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), PersonId.New(), EditionId.New());
        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CancelOwnTicketCommand(ticket.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketNotFound_ThrowsNotFound()
    {
        _ticketRepo.GetByIdAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>()).Returns((Ticket?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new CancelOwnTicketCommand(Guid.NewGuid()), default));
    }
}
