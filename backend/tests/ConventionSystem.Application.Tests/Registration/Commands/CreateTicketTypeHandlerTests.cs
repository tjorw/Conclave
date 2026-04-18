using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CreateTicketType;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CreateTicketTypeHandlerTests
{
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreateTicketTypeHandler _handler;

    public CreateTicketTypeHandlerTests()
    {
        _handler = new CreateTicketTypeHandler(_ticketTypeRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTicketTypeId()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        var id = await _handler.Handle(
            new CreateTicketTypeCommand(edition.Id.Value, "Helgbiljett", 15000, TicketTypeCategory.Visitor), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(
            new CreateTicketTypeCommand(edition.Id.Value, "Helgbiljett", 15000, TicketTypeCategory.Visitor), default);

        await _ticketTypeRepo.Received(1).AddAndSaveAsync(
            Arg.Any<Domain.Registration.Entities.TicketType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidDaysInPeriod_Succeeds()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var days = new[] { new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 2) };

        var id = await _handler.Handle(
            new CreateTicketTypeCommand(edition.Id.Value, "Dagsbiljett", 5000, TicketTypeCategory.Visitor, days), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidDayOutsidePeriod_ThrowsTicketValidDaysOutsideEditionPeriodException()
    {
        var (_, admin, edition) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var days = new[] { new DateOnly(2027, 3, 4) };

        await Assert.ThrowsAsync<TicketValidDaysOutsideEditionPeriodException>(
            () => _handler.Handle(
                new CreateTicketTypeCommand(edition.Id.Value, "Biljett", 0, TicketTypeCategory.Visitor, days), default));
    }

    [Fact]
    public async Task Handle_EditionNotFound_Throws()
    {
        _editionRepo.GetByIdAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new CreateTicketTypeCommand(Guid.NewGuid(), "Biljett", 0, TicketTypeCategory.Visitor), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdmin_ThrowsForbiddenException()
    {
        var (convention, _, edition) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new CreateTicketTypeCommand(edition.Id.Value, "Biljett", 0, TicketTypeCategory.Visitor), default));
    }
}
