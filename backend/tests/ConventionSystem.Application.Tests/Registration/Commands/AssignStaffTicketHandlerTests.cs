using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AssignStaffTicket;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AssignStaffTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AssignStaffTicketHandler _handler;

    public AssignStaffTicketHandlerTests()
    {
        _handler = new AssignStaffTicketHandler(
            _ticketRepo,
            _ticketTypeRepo,
            _editionRepo,
            _conventionRepo,
            _personRepo,
            _currentUser);
    }

    [Fact]
    public async Task Handle_AssignsNewStaffTicket_CreatesTicket()
    {
        var setup = Setup();
        var ticketTypeId = TicketTypeId.New();
        var ticketType = new TicketType(ticketTypeId, setup.edition.Id, "Funktionär", 500, TicketTypeCategory.Staff);

        _ticketRepo.ListActiveStaffTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>())
            .Returns(ticketType);

        await _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            ticketTypeId.Value), default);

        _ticketRepo.Received(1).Add(
            Arg.Is<Ticket>(t =>
                t.PersonId == setup.staffMember.Id &&
                t.EditionId == setup.edition.Id &&
                t.TicketTypeId == ticketTypeId &&
                t.AssignedById == setup.staffCoord.Id &&
                t.Status == TicketStatus.Reserved));
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DifferentTicketType_RevokesCurrentAndCreatesNew()
    {
        var setup = Setup();
        var oldTypeId = TicketTypeId.New();
        var newTypeId = TicketTypeId.New();
        var currentTicket = new Ticket(TicketId.New(), oldTypeId, setup.staffMember.Id, setup.edition.Id, setup.staffCoord.Id);
        var newTicketType = new TicketType(newTypeId, setup.edition.Id, "Funktionär VIP", 500, TicketTypeCategory.Staff);

        _ticketRepo.ListActiveStaffTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([currentTicket]);
        _ticketTypeRepo.GetByIdAsync(newTypeId, Arg.Any<CancellationToken>())
            .Returns(newTicketType);

        await _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            newTypeId.Value), default);

        Assert.Equal(TicketStatus.Revoked, currentTicket.Status);
        _ticketRepo.Received(1).Add(
            Arg.Is<Ticket>(t =>
                t.PersonId == setup.staffMember.Id &&
                t.TicketTypeId == newTypeId &&
                t.Status == TicketStatus.Reserved));
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FreeTicketType_AutoConfirmsTicket()
    {
        var setup = Setup();
        var ticketTypeId = TicketTypeId.New();
        var ticketType = new TicketType(ticketTypeId, setup.edition.Id, "Funktionär (gratis)", 0, TicketTypeCategory.Staff);

        _ticketRepo.ListActiveStaffTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>())
            .Returns(ticketType);

        await _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            ticketTypeId.Value), default);

        _ticketRepo.Received(1).Add(
            Arg.Is<Ticket>(t =>
                t.TicketTypeId == ticketTypeId &&
                t.Status == TicketStatus.Paid));
    }

    [Fact]
    public async Task Handle_SameTicketType_IsIdempotent()
    {
        var setup = Setup();
        var ticketTypeId = TicketTypeId.New();
        var currentTicket = new Ticket(TicketId.New(), ticketTypeId, setup.staffMember.Id, setup.edition.Id, setup.staffCoord.Id);
        var ticketType = new TicketType(ticketTypeId, setup.edition.Id, "Funktionär", 0, TicketTypeCategory.Staff);

        _ticketRepo.ListActiveStaffTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([currentTicket]);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>())
            .Returns(ticketType);

        await _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            ticketTypeId.Value), default);

        _ticketRepo.DidNotReceive().Add(Arg.Any<Ticket>());
        await _ticketRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullTicketType_RevokesCurrentWithoutCreatingNew()
    {
        var setup = Setup();
        var currentTicket = new Ticket(TicketId.New(), TicketTypeId.New(), setup.staffMember.Id, setup.edition.Id, setup.staffCoord.Id);

        _ticketRepo.ListActiveStaffTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([currentTicket]);

        await _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            null), default);

        Assert.Equal(TicketStatus.Revoked, currentTicket.Status);
        _ticketRepo.DidNotReceive().Add(Arg.Any<Ticket>());
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongTicketTypeCategory_Throws()
    {
        var setup = Setup();
        var ticketTypeId = TicketTypeId.New();
        var visitorTicketType = new TicketType(ticketTypeId, setup.edition.Id, "Besökare", 0, TicketTypeCategory.Visitor);

        _ticketRepo.ListActiveStaffTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>())
            .Returns(visitorTicketType);

        await Assert.ThrowsAsync<DomainRuleViolationException>(() => _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            ticketTypeId.Value), default));
    }

    [Fact]
    public async Task Handle_NonStaffCoordinatorAndNonAdmin_Throws()
    {
        var setup = Setup(staffCoordCurrentUser: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(new AssignStaffTicketCommand(
            setup.edition.Id.Value,
            setup.staffMember.Id.Value,
            null), default));
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Aggregates.Edition edition, Domain.Convention.Entities.Person staffCoord, Domain.Convention.Entities.Person staffMember) Setup(bool staffCoordCurrentUser = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff Coord", "staffcoord@example.com");
        var eventCoord = convention.CreatePerson("Event Coord", "eventcoord@example.com");
        var staffMember = convention.CreatePerson("Funktionär", "staff@example.com");
        var outsider = convention.CreatePerson("Utomstående", "outsider@example.com");
        var edition = convention.CreateEdition(
            "Konvent 2027",
            new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3)),
            staffCoord.Id,
            eventCoord.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(staffMember.Id, Arg.Any<CancellationToken>()).Returns(staffMember);
        _currentUser.PersonId.Returns(staffCoordCurrentUser ? staffCoord.Id : outsider.Id);

        return (convention, edition, staffCoord, staffMember);
    }
}
