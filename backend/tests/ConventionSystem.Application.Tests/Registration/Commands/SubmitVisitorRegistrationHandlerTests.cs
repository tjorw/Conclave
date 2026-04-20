using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.SubmitVisitorRegistration;
using ConventionSystem.Domain.Common;
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
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SubmitVisitorRegistrationHandler _handler;

    public SubmitVisitorRegistrationHandlerTests()
    {
        _handler = new SubmitVisitorRegistrationHandler(_registrationRepo, _ticketRepo, _ticketTypeRepo, _editionRepo, _personRepo, _currentUser);
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

        var ticketType = new TicketType(TicketTypeId.New(), edition.Id, "Helgbiljett", 15000, TicketTypeCategory.Visitor);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        _ticketTypeRepo.GetByIdAsync(ticketType.Id, Arg.Any<CancellationToken>()).Returns(ticketType);
        _registrationRepo.HasActiveRegistrationForTicketTypeAsync(person.Id, edition.Id, ticketType.Id, Arg.Any<CancellationToken>()).Returns(false);
        _currentUser.PersonId.Returns(person.Id);

        return (convention, person, edition, ticketType);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsRegistrationId()
    {
        var (_, person, edition, ticketType) = Setup();

        var id = await _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, ticketType.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsTicketAndRegistration()
    {
        var (_, person, edition, ticketType) = Setup();

        await _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, ticketType.Id.Value), default);

        await _ticketRepo.Received(1).AddAsync(Arg.Any<Domain.Registration.Aggregates.Ticket>(), Arg.Any<CancellationToken>());
        await _registrationRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Registration.Aggregates.VisitorRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotOpen_Throws()
    {
        var (_, person, edition, ticketType) = Setup(visitorRegOpen: false);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_DuplicateTicketType_Throws()
    {
        var (_, person, edition, ticketType) = Setup();
        _registrationRepo.HasActiveRegistrationForTicketTypeAsync(person.Id, edition.Id, ticketType.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(
                new SubmitVisitorRegistrationCommand(edition.Id.Value, ticketType.Id.Value),
                default));
    }

    [Fact]
    public async Task Handle_AdditionalDifferentTicketType_AllowsRegistration()
    {
        var (_, person, edition, ticketType) = Setup();
        var otherTicketType = new TicketType(TicketTypeId.New(), edition.Id, "Dagbiljett", 8000, TicketTypeCategory.Visitor);
        _ticketTypeRepo.GetByIdAsync(otherTicketType.Id, Arg.Any<CancellationToken>()).Returns(otherTicketType);
        _registrationRepo.HasActiveRegistrationForTicketTypeAsync(person.Id, edition.Id, otherTicketType.Id, Arg.Any<CancellationToken>()).Returns(false);

        var id = await _handler.Handle(
            new SubmitVisitorRegistrationCommand(edition.Id.Value, otherTicketType.Id.Value),
            default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_WrongTicketTypeCategory_Throws()
    {
        var (_, person, edition, _) = Setup();
        var staffTicketType = new TicketType(TicketTypeId.New(), edition.Id, "Staff-biljett", 0, TicketTypeCategory.Staff);
        _ticketTypeRepo.GetByIdAsync(staffTicketType.Id, Arg.Any<CancellationToken>()).Returns(staffTicketType);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, staffTicketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_InactivePerson_Throws()
    {
        var (convention, person, edition, ticketType) = Setup();
        convention.DeactivatePerson(person);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, ticketType.Id.Value), default));
    }

    [Fact]
    public async Task Handle_FreeTicketType_AutoConfirmsTicketAndRegistration()
    {
        var (_, person, edition, _) = Setup();
        var freeTicketType = new TicketType(TicketTypeId.New(), edition.Id, "Gratisbiljett", 0, TicketTypeCategory.Visitor);
        _ticketTypeRepo.GetByIdAsync(freeTicketType.Id, Arg.Any<CancellationToken>()).Returns(freeTicketType);
        _registrationRepo.HasActiveRegistrationForTicketTypeAsync(person.Id, edition.Id, freeTicketType.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        Domain.Registration.Aggregates.Ticket? createdTicket = null;
        _ticketRepo
            .When(repo => repo.AddAsync(Arg.Any<Domain.Registration.Aggregates.Ticket>(), Arg.Any<CancellationToken>()))
            .Do(call => createdTicket = call.ArgAt<Domain.Registration.Aggregates.Ticket>(0));

        await _handler.Handle(new SubmitVisitorRegistrationCommand(edition.Id.Value, freeTicketType.Id.Value), default);

        Assert.NotNull(createdTicket);
        Assert.Equal(TicketStatus.Paid, createdTicket!.Status);
        await _registrationRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Registration.Aggregates.VisitorRegistration>(registration =>
                registration.Status == VisitorRegistrationStatus.Confirmed
                && registration.PaymentReference == "AUTO-FREE"),
            Arg.Any<CancellationToken>());
    }
}
