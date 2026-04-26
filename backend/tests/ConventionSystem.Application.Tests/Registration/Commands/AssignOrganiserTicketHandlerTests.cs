using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AssignOrganiserTicket;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AssignOrganiserTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AssignOrganiserTicketHandler _handler;

    public AssignOrganiserTicketHandlerTests()
    {
        _handler = new AssignOrganiserTicketHandler(
            _ticketRepo,
            _ticketTypeRepo,
            _editionRepo,
            _conventionRepo,
            _personRepo,
            _currentUser);
    }

    [Fact]
    public async Task Handle_DifferentTicketType_RevokesCurrentAndCreatesNew()
    {
        var setup = Setup();
        var oldTypeId = TicketTypeId.New();
        var newTypeId = TicketTypeId.New();
        var currentTicket = new Ticket(TicketId.New(), oldTypeId, setup.organiser.Id, setup.edition.Id, setup.admin.Id);
        var newTicketType = new TicketType(newTypeId, setup.edition.Id, "Arrangör", 0, TicketTypeCategory.Organiser);

        _ticketRepo.ListActiveOrganiserTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([currentTicket]);
        _ticketTypeRepo.GetByIdAsync(newTypeId, Arg.Any<CancellationToken>())
            .Returns(newTicketType);

        await _handler.Handle(new AssignOrganiserTicketCommand(
            setup.edition.Id.Value,
            setup.organiser.Id.Value,
            newTypeId.Value), default);

        Assert.Equal(TicketStatus.Revoked, currentTicket.Status);
        _ticketRepo.Received(1).Add(
            Arg.Is<Ticket>(t =>
                t.PersonId == setup.organiser.Id &&
                t.EditionId == setup.edition.Id &&
                t.TicketTypeId == newTypeId &&
                t.AssignedById == setup.admin.Id &&
                t.Status == TicketStatus.Reserved));
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullTicketType_RevokesCurrentWithoutCreatingNew()
    {
        var setup = Setup();
        var currentTicket = new Ticket(TicketId.New(), TicketTypeId.New(), setup.organiser.Id, setup.edition.Id, setup.admin.Id);

        _ticketRepo.ListActiveOrganiserTicketsAsync(setup.edition.Id, Arg.Any<IReadOnlyCollection<PersonId>>(), Arg.Any<CancellationToken>())
            .Returns([currentTicket]);

        await _handler.Handle(new AssignOrganiserTicketCommand(
            setup.edition.Id.Value,
            setup.organiser.Id.Value,
            null), default);

        Assert.Equal(TicketStatus.Revoked, currentTicket.Status);
        _ticketRepo.DidNotReceive().Add(Arg.Any<Ticket>());
        await _ticketRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var setup = Setup(adminCurrentUser: false);

        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(new AssignOrganiserTicketCommand(
            setup.edition.Id.Value,
            setup.organiser.Id.Value,
            null), default));
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Aggregates.Edition edition, Domain.Convention.Entities.Person admin, Domain.Convention.Entities.Person organiser) Setup(bool adminCurrentUser = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var outsider = convention.CreatePerson("Utomstående", "outsider@example.com");
        var edition = convention.CreateEdition(
            "Konvent 2027",
            new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3)),
            staffCoord.Id,
            eventCoord.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(organiser.Id, Arg.Any<CancellationToken>()).Returns(organiser);
        _currentUser.PersonId.Returns(adminCurrentUser ? admin.Id : outsider.Id);

        return (convention, edition, admin, organiser);
    }
}
