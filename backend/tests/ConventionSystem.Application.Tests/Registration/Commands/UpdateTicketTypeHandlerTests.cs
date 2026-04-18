using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.UpdateTicketType;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class UpdateTicketTypeHandlerTests
{
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateTicketTypeHandler _handler;

    public UpdateTicketTypeHandlerTests()
    {
        _handler = new UpdateTicketTypeHandler(_ticketTypeRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             TicketType ticketType) Setup()
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

        return (convention, admin, ticketType);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTicketType()
    {
        var (_, admin, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateTicketTypeCommand(ticketType.Id.Value, "Nytt namn", 20000), default);

        Assert.Equal("Nytt namn", ticketType.Name);
        Assert.Equal(20000, ticketType.Price);
        Assert.Null(ticketType.ValidDays);
        await _ticketTypeRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidDaysInPeriod_UpdatesValidDays()
    {
        var (_, admin, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var days = new[] { new DateOnly(2027, 3, 1) };

        await _handler.Handle(new UpdateTicketTypeCommand(ticketType.Id.Value, "Biljett", 0, days), default);

        Assert.Equal(days, ticketType.ValidDays);
    }

    [Fact]
    public async Task Handle_ValidDayOutsidePeriod_ThrowsTicketValidDaysOutsideEditionPeriodException()
    {
        var (_, admin, ticketType) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        var days = new[] { new DateOnly(2027, 3, 5) };

        await Assert.ThrowsAsync<TicketValidDaysOutsideEditionPeriodException>(
            () => _handler.Handle(new UpdateTicketTypeCommand(ticketType.Id.Value, "Biljett", 0, days), default));
    }

    [Fact]
    public async Task Handle_TicketTypeNotFound_Throws()
    {
        _ticketTypeRepo.GetByIdAsync(Arg.Any<TicketTypeId>(), Arg.Any<CancellationToken>())
            .Returns((TicketType?)null);

        await Assert.ThrowsAsync<TicketTypeNotFoundException>(
            () => _handler.Handle(new UpdateTicketTypeCommand(Guid.NewGuid(), "Namn", 0), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdmin_ThrowsForbiddenException()
    {
        var (convention, _, ticketType) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new UpdateTicketTypeCommand(ticketType.Id.Value, "Namn", 0), default));
    }
}
