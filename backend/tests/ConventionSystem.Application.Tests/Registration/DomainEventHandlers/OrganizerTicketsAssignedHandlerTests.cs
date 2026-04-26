using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.DomainEventHandlers;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Event.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.DomainEventHandlers;

public class OrganizerTicketsAssignedHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly OrganizerTicketsAssignedHandler _handler;

    public OrganizerTicketsAssignedHandlerTests()
    {
        _handler = new OrganizerTicketsAssignedHandler(_ticketRepo, _ticketTypeRepo);
    }

    [Fact]
    public async Task Handle_DifferentTicketType_RevokesCurrentAndCreatesNew()
    {
        var editionId = EditionId.New();
        var personId = PersonId.New();
        var performedById = PersonId.New();
        var oldTypeId = TicketTypeId.New();
        var newTypeId = TicketTypeId.New();
        var currentTicket = new Ticket(TicketId.New(), oldTypeId, personId, editionId, performedById);
        var newTicketType = new TicketType(newTypeId, editionId, "Arrangör", 0, TicketTypeCategory.Organiser);

        _ticketRepo.ListActiveOrganiserTicketsAsync(editionId, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([currentTicket]);
        _ticketTypeRepo.GetByIdAsync(newTypeId, Arg.Any<CancellationToken>())
            .Returns(newTicketType);

        await _handler.Handle(new OrganizerTicketsAssigned(
            EventId.New(),
            editionId,
            performedById,
            [new OrganizerTicketAssignment(personId, newTypeId)],
            DateTimeOffset.UtcNow));

        Assert.Equal(TicketStatus.Revoked, currentTicket.Status);
        _ticketRepo.Received(1).Add(
            Arg.Is<Ticket>(t =>
                t.PersonId == personId &&
                t.EditionId == editionId &&
                t.TicketTypeId == newTypeId &&
                t.AssignedById == performedById &&
                t.Status == TicketStatus.Reserved));
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonOrganiserTicketType_Throws()
    {
        var editionId = EditionId.New();
        var personId = PersonId.New();
        var ticketTypeId = TicketTypeId.New();
        var visitorTicketType = new TicketType(ticketTypeId, editionId, "Besökare", 10000, TicketTypeCategory.Visitor);

        _ticketRepo.ListActiveOrganiserTicketsAsync(editionId, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>())
            .Returns(visitorTicketType);

        await Assert.ThrowsAsync<DomainRuleViolationException>(() => _handler.Handle(new OrganizerTicketsAssigned(
            EventId.New(),
            editionId,
            PersonId.New(),
            [new OrganizerTicketAssignment(personId, ticketTypeId)],
            DateTimeOffset.UtcNow)));
    }
}
