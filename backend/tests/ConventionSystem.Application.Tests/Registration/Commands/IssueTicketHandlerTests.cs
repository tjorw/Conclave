using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.IssueTicket;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class IssueTicketHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IssueTicketHandler _handler;

    public IssueTicketHandlerTests()
    {
        _handler = new IssueTicketHandler(_ticketRepo, _ticketTypeRepo, _editionRepo, _conventionRepo, _personRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             TicketType ticketType) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);

        var ticketTypeId = TicketTypeId.New();
        var ticketType = new TicketType(ticketTypeId, edition.Id, "Standardbiljett", 50000, TicketTypeCategory.Visitor);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>()).Returns(ticketType);

        return (convention, admin, edition, ticketType);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTicketId()
    {
        var (_, admin, edition, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var id = await _handler.Handle(
            new IssueTicketCommand(admin.Id.Value, edition.Id.Value, ticketType.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var (_, admin, edition, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new IssueTicketCommand(admin.Id.Value, edition.Id.Value, ticketType.Id.Value), default);

        await _ticketRepo.Received(1).AddAndSaveAsync(
            Arg.Any<Domain.Registration.Aggregates.Ticket>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsForbiddenException()
    {
        var (convention, _, edition, ticketType) = Setup();
        var nonAdmin = convention.CreatePerson("Annan", "annan@example.com");
        _personRepo.GetByIdAsync(nonAdmin.Id, Arg.Any<CancellationToken>()).Returns(nonAdmin);
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new IssueTicketCommand(nonAdmin.Id.Value, edition.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_EditionNotFound_ThrowsResourceNotFoundException()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new IssueTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
