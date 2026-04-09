using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.ConfirmVisitorRegistrationPayment;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class ConfirmVisitorRegistrationPaymentHandlerTests
{
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ConfirmVisitorRegistrationPaymentHandler _handler;

    public ConfirmVisitorRegistrationPaymentHandlerTests()
    {
        _handler = new ConfirmVisitorRegistrationPaymentHandler(_registrationRepo, _ticketRepo);
    }

    private (VisitorRegistration registration, Ticket ticket) Setup()
    {
        var ticketId = TicketId.New();
        var registration = new VisitorRegistration(VisitorRegistrationId.New(), PersonId.New(), EditionId.New(), ticketId);
        var ticket = new Ticket(ticketId, TicketTypeId.New(), PersonId.New(), EditionId.New());

        _registrationRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _ticketRepo.GetByIdAsync(ticketId, Arg.Any<CancellationToken>()).Returns(ticket);

        return (registration, ticket);
    }

    [Fact]
    public async Task Handle_ValidCommand_ConfirmsBoth()
    {
        var (registration, ticket) = Setup();

        await _handler.Handle(new ConfirmVisitorRegistrationPaymentCommand(registration.Id.Value, "EXT-123", Guid.NewGuid()), default);

        Assert.Equal(Domain.Registration.Enums.VisitorRegistrationStatus.Confirmed, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Paid, ticket.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (registration, _) = Setup();

        await _handler.Handle(new ConfirmVisitorRegistrationPaymentCommand(registration.Id.Value, "EXT-123", Guid.NewGuid()), default);

        await _registrationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _registrationRepo.GetByIdAsync(Arg.Any<VisitorRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((VisitorRegistration?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new ConfirmVisitorRegistrationPaymentCommand(Guid.NewGuid(), "EXT", Guid.NewGuid()), default));
    }
}
