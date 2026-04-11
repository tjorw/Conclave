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
    private readonly CancelSessionRegistrationHandler _handler;

    public CancelSessionRegistrationHandlerTests()
    {
        _handler = new CancelSessionRegistrationHandler(_sessionRegRepo);
    }

    [Fact]
    public async Task Handle_ValidCommand_CancelsRegistration()
    {
        var registration = new SessionRegistration(
            SessionRegistrationId.New(), SessionId.New(), PersonId.New(), TicketId.New());
        _sessionRegRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        await _handler.Handle(new CancelSessionRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.SessionRegistrationStatus.Cancelled, registration.Status);
        await _sessionRegRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _sessionRegRepo.GetByIdAsync(Arg.Any<SessionRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((SessionRegistration?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CancelSessionRegistrationCommand(Guid.NewGuid()), default));
    }
}
