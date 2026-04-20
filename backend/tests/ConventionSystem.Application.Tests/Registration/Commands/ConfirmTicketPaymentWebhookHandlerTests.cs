using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.ConfirmTicketPaymentWebhook;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class ConfirmTicketPaymentWebhookHandlerTests
{
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ConfirmTicketPaymentWebhookHandler _handler;

    public ConfirmTicketPaymentWebhookHandlerTests()
    {
        _handler = new ConfirmTicketPaymentWebhookHandler(_registrationRepo, _ticketRepo);
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
    public async Task Handle_SuccessStatus_ConfirmsTicketAndRegistration()
    {
        var (registration, ticket) = Setup();

        await _handler.Handle(new ConfirmTicketPaymentWebhookCommand(registration.Id.Value, "EXT-200", "Succeeded"), default);

        Assert.Equal(VisitorRegistrationStatus.Confirmed, registration.Status);
        Assert.Equal(TicketStatus.Paid, ticket.Status);
        await _registrationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateWebhook_IsIdempotent()
    {
        var (registration, ticket) = Setup();
        registration.ConfirmPayment("EXT-200");
        ticket.ConfirmPayment();

        await _handler.Handle(new ConfirmTicketPaymentWebhookCommand(registration.Id.Value, "EXT-200", "Paid"), default);

        await _registrationRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FailedStatus_DoesNothing()
    {
        var (registration, _) = Setup();

        await _handler.Handle(new ConfirmTicketPaymentWebhookCommand(registration.Id.Value, "EXT-200", "Failed"), default);

        Assert.Equal(VisitorRegistrationStatus.PendingPayment, registration.Status);
        await _registrationRepo.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownRegistration_ThrowsNotFound()
    {
        _registrationRepo.GetByIdAsync(Arg.Any<VisitorRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((VisitorRegistration?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new ConfirmTicketPaymentWebhookCommand(Guid.NewGuid(), "EXT", "Paid"), default));
    }
}
