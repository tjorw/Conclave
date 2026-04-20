using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CancelSessionRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class CancelSessionRegistrationHandlerTests
{
    private readonly ISessionRegistrationRepository _sessionRegRepo = Substitute.For<ISessionRegistrationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelSessionRegistrationHandler _handler;

    public CancelSessionRegistrationHandlerTests()
    {
        _handler = new CancelSessionRegistrationHandler(_sessionRegRepo, _currentUser);
    }

    private SessionRegistration SetupRegistration()
    {
        var registration = new SessionRegistration(
            SessionRegistrationId.New(), SessionId.New(), PersonId.New(), TicketId.New());
        _sessionRegRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _currentUser.PersonId.Returns(registration.PersonId);
        _currentUser.IsAdmin.Returns(false);
        return registration;
    }

    [Fact]
    public async Task Handle_ValidCommand_CancelsRegistration()
    {
        var registration = SetupRegistration();

        await _handler.Handle(new CancelSessionRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.SessionRegistrationStatus.Cancelled, registration.Status);
        await _sessionRegRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _sessionRegRepo.GetByIdAsync(Arg.Any<SessionRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((SessionRegistration?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new CancelSessionRegistrationCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_OtherUserNotAdmin_Throws()
    {
        var registration = SetupRegistration();
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(false);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CancelSessionRegistrationCommand(registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AdminCanCancelOtherUsersRegistration()
    {
        var registration = SetupRegistration();
        _currentUser.PersonId.Returns(PersonId.New());
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new CancelSessionRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.SessionRegistrationStatus.Cancelled, registration.Status);
    }
}
