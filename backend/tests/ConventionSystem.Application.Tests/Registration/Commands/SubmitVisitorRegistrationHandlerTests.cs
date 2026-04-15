using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class SubmitVisitorRegistrationHandlerTests
{
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly SubmitVisitorRegistrationHandler _handler;

    public SubmitVisitorRegistrationHandlerTests()
    {
        _handler = new SubmitVisitorRegistrationHandler(_registrationRepo, _ticketRepo, _ticketTypeRepo, _editionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person person,
             Domain.Convention.Aggregates.Edition edition,
             TicketType ticketType) Setup(bool visitorRegOpen = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var person = convention.CreatePerson("Besökare", "visitor@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        edition.Publish(admin.Id);
        if (visitorRegOpen)
            edition.OpenVisitorRegistration(admin.Id);

        var ticketType = new TicketType(TicketTypeId.New(), edition.Id, "Helgbiljett", 15000, TicketTypeCategory.Visitor, true, true);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        _ticketTypeRepo.GetByIdAsync(ticketType.Id, Arg.Any<CancellationToken>()).Returns(ticketType);
        _registrationRepo.HasActiveRegistrationAsync(person.Id, edition.Id, Arg.Any<CancellationToken>()).Returns(false);

        return (convention, person, edition, ticketType);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegistrationId()
    {
        var (_, person, edition, ticketType) = Setup();

        var id = await _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, person.Id.Value, ticketType.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsTicketAndRegistration()
    {
        var (_, person, edition, ticketType) = Setup();

        await _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, person.Id.Value, ticketType.Id.Value), default);

        await _ticketRepo.Received(1).AddAsync(Arg.Any<Domain.Registration.Aggregates.Ticket>(), Arg.Any<CancellationToken>());
        await _registrationRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Registration.Aggregates.VisitorRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotOpen_Throws()
    {
        var (_, person, edition, ticketType) = Setup(visitorRegOpen: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, person.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_DuplicateRegistration_Throws()
    {
        var (_, person, edition, ticketType) = Setup();
        _registrationRepo.HasActiveRegistrationAsync(person.Id, edition.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, person.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_WrongTicketTypeCategory_Throws()
    {
        var (_, person, edition, _) = Setup();
        var staffTicketType = new TicketType(TicketTypeId.New(), edition.Id, "Staff-biljett", 0, TicketTypeCategory.Staff, false, false);
        _ticketTypeRepo.GetByIdAsync(staffTicketType.Id, Arg.Any<CancellationToken>()).Returns(staffTicketType);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, person.Id.Value, staffTicketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_InactivePerson_Throws()
    {
        var (convention, person, edition, ticketType) = Setup();
        convention.DeactivatePerson(person);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, person.Id.Value, ticketType.Id.Value), default));
    }
}
