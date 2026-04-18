using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.DeleteTicketType;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class DeleteTicketTypeHandlerTests
{
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly DeleteTicketTypeHandler _handler;

    public DeleteTicketTypeHandlerTests()
    {
        _handler = new DeleteTicketTypeHandler(_ticketTypeRepo, _ticketRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             TicketType ticketType) Setup(bool hasIssuedTickets = false)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        var ticketType = new TicketType(TicketTypeId.New(), edition.Id, "Helgbiljett", 15000, TicketTypeCategory.Visitor);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _ticketTypeRepo.GetByIdAsync(ticketType.Id, Arg.Any<CancellationToken>()).Returns(ticketType);
        _ticketRepo.ExistsByTypeAsync(ticketType.Id, Arg.Any<CancellationToken>()).Returns(hasIssuedTickets);

        return (convention, admin, ticketType);
    }

    [Fact]
    public async Task Handle_NoIssuedTickets_DeletesTicketType()
    {
        var (_, admin, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new DeleteTicketTypeCommand(ticketType.Id.Value), default);

        await _ticketTypeRepo.Received(1).DeleteAndSaveAsync(ticketType, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HasIssuedTickets_Throws()
    {
        var (_, admin, ticketType) = Setup(hasIssuedTickets: true);
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<TicketTypeHasIssuedTicketsException>(
            () => _handler.Handle(new DeleteTicketTypeCommand(ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TicketTypeNotFound_Throws()
    {
        _ticketTypeRepo.GetByIdAsync(Arg.Any<TicketTypeId>(), Arg.Any<CancellationToken>())
            .Returns((TicketType?)null);

        await Assert.ThrowsAsync<TicketTypeNotFoundException>(
            () => _handler.Handle(new DeleteTicketTypeCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdmin_Throws()
    {
        var (convention, _, ticketType) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new DeleteTicketTypeCommand(ticketType.Id.Value), default));
    }
}
