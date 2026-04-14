using ConventionSystem.Application.Common;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CancelVisitorRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CancelVisitorRegistrationHandlerTests
{
    private readonly IVisitorRegistrationRepository _registrationRepo = Substitute.For<IVisitorRegistrationRepository>();
    private readonly ITicketRepository _ticketRepo = Substitute.For<ITicketRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelVisitorRegistrationHandler _handler;

    public CancelVisitorRegistrationHandlerTests()
    {
        _handler = new CancelVisitorRegistrationHandler(_registrationRepo, _ticketRepo, _currentUser);
    }

    private (VisitorRegistration registration, Ticket ticket) Setup()
    {
        var ticketId = TicketId.New();
        var registration = new VisitorRegistration(VisitorRegistrationId.New(), PersonId.New(), EditionId.New(), ticketId);
        var ticket = new Ticket(ticketId, TicketTypeId.New(), PersonId.New(), EditionId.New());

        _registrationRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _ticketRepo.GetByIdAsync(ticketId, Arg.Any<CancellationToken>()).Returns(ticket);
        _currentUser.PersonId.Returns(registration.PersonId);
        _currentUser.IsAdmin.Returns(false);

        return (registration, ticket);
    }

    [Fact]
    public async Task Handle_ValidCommand_CancelsBoth()
    {
        var (registration, ticket) = Setup();

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.VisitorRegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Revoked, ticket.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (registration, _) = Setup();

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        await _registrationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _registrationRepo.GetByIdAsync(Arg.Any<VisitorRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((VisitorRegistration?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CancelVisitorRegistrationCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_OtherUserNotAdmin_Throws()
    {
        var (registration, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AdminCanCancelOtherUsersRegistration()
    {
        var (registration, ticket) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new CancelVisitorRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.VisitorRegistrationStatus.Cancelled, registration.Status);
        Assert.Equal(Domain.Registration.Enums.TicketStatus.Revoked, ticket.Status);
    }
}
