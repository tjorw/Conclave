using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RegisterManualTicketPayment;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Exceptions;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RegisterManualTicketPaymentHandlerTests
{
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RegisterManualTicketPaymentHandler _handler;

    public RegisterManualTicketPaymentHandlerTests()
    {
        _handler = new RegisterManualTicketPaymentHandler(_ticketRepo, _registrationRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Ticket ticket,
             VisitorRegistration registration) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);
        var ticket = new Ticket(TicketId.New(), TicketTypeId.New(), admin.Id, edition.Id, admin.Id);
        var registration = new VisitorRegistration(VisitorRegistrationId.New(), admin.Id, edition.Id, ticket.Id);

        _ticketRepo.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        _registrationRepo.GetByTicketIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, ticket, registration);
    }

    [Fact]
    public async Task Handle_AdminCommand_ConfirmsTicketAndRegistration()
    {
        var (_, admin, _, ticket, registration) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RegisterManualTicketPaymentCommand(ticket.Id.Value, "MAN-123"), default);

        Assert.Equal(TicketStatus.Paid, ticket.Status);
        Assert.Equal(VisitorRegistrationStatus.Confirmed, registration.Status);
        Assert.Equal("MAN-123", registration.PaymentReference);
    }

    [Fact]
    public async Task Handle_AdminCommand_CallsSaveThroughRegistrationRepo()
    {
        var (_, admin, _, ticket, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RegisterManualTicketPaymentCommand(ticket.Id.Value, null), default);

        await _registrationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsForbidden()
    {
        var (convention, _, _, ticket, _) = Setup();
        var nonAdmin = convention.CreatePerson("Person", "person@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new RegisterManualTicketPaymentCommand(ticket.Id.Value, "X"), default));
    }

    [Fact]
    public async Task Handle_AlreadyPaid_ThrowsDomainException()
    {
        var (_, admin, _, ticket, registration) = Setup();
        _currentUser.PersonId.Returns(admin.Id);
        registration.ConfirmPayment("FIRST");
        ticket.ConfirmPayment();

        await Assert.ThrowsAsync<TicketAlreadyPaidException>(
            () => _handler.Handle(new RegisterManualTicketPaymentCommand(ticket.Id.Value, "SECOND"), default));
    }
}
