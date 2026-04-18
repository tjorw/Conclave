using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CancelVisitorRegistrationHandlerTests
{
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelVisitorRegistrationHandler _handler;

    public CancelVisitorRegistrationHandlerTests()
    {
        _handler = new CancelVisitorRegistrationHandler(_registrationRepo, _ticketRepo, _ticketTypeRepo, _currentUser);
    }

    private (VisitorRegistration registration, Ticket ticket, TicketType ticketType) Setup(int ticketPrice = 15000)
    {
        var editionId = EditionId.New();
        var ticketId = TicketId.New();
        var ticketTypeId = TicketTypeId.New();
        var registration = new VisitorRegistration(VisitorRegistrationId.New(), PersonId.New(), editionId, ticketId);
        var ticket = new Ticket(ticketId, ticketTypeId, registration.PersonId, editionId);
        var ticketType = new TicketType(ticketTypeId, editionId, "Biljett", ticketPrice, TicketTypeCategory.Visitor);

        _registrationRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _ticketRepo.GetByIdAsync(ticketId, Arg.Any<CancellationToken>()).Returns(ticket);
        _ticketTypeRepo.GetByIdAsync(ticketTypeId, Arg.Any<CancellationToken>()).Returns(ticketType);
        _currentUser.PersonId.Returns(registration.PersonId);
        _currentUser.IsAdmin.Returns(false);

        return (registration, ticket, ticketType);
    }

    [Fact]
    public async Task Handle_ValidCommand_CancelsBoth()
    {
        var (registration, ticket, _) = Setup();

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.VisitorRegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Revoked, ticket.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (registration, _, _) = Setup();

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        await _registrationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _registrationRepo.GetByIdAsync(Arg.Any<VisitorRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((VisitorRegistration?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new CancelVisitorRegistrationCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_OtherUserNotAdmin_Throws()
    {
        var (registration, _, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(false);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AdminCanCancelOtherUsersRegistration()
    {
        var (registration, ticket, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.VisitorRegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Revoked, ticket.Status);
    }

    [Fact]
    public async Task Handle_OwnerCannotCancelConfirmedRegistration_ThrowsForbidden()
    {
        var (registration, _, _) = Setup();
        registration.ConfirmPayment("manual-ref");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AdminCanCancelConfirmedRegistration()
    {
        var (registration, ticket, _) = Setup();
        registration.ConfirmPayment("manual-ref");
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(VisitorRegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Revoked, ticket.Status);
    }

    [Fact]
    public async Task Handle_OwnerCanCancelConfirmedFreeRegistration()
    {
        var (registration, ticket, _) = Setup(ticketPrice: 0);
        registration.ConfirmPayment("AUTO-FREE");

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(VisitorRegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Revoked, ticket.Status);
    }
}
