using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.ConfirmTeamRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public sealed class ConfirmTeamRegistrationHandlerTests
{
    private readonly ITeamEventRegistrationRepository _regRepo = Substitute.For<ITeamEventRegistrationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ConfirmTeamRegistrationHandler _handler;

    public ConfirmTeamRegistrationHandlerTests()
    {
        _handler = new ConfirmTeamRegistrationHandler(_regRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (TeamEventRegistration, Domain.Convention.Aggregates.Convention) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@test.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@test.com");
        var eventCoord = convention.CreatePerson("Event", "event@test.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);

        var registration = new TeamEventRegistration(
            TeamEventRegistrationId.New(), TeamId.New(), EventId.New(), edition.Id);

        _currentUser.PersonId.Returns(admin.Id);
        _regRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (registration, convention);
    }

    [Fact]
    public async Task Handle_AdminConfirms_SetsStatusToConfirmed()
    {
        var (registration, _) = Setup();

        await _handler.Handle(new ConfirmTeamRegistrationCommand(registration.Id.Value), default);

        Assert.Equal(TeamRegistrationStatus.Confirmed, registration.Status);
        await _regRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _regRepo.GetByIdAsync(Arg.Any<TeamEventRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((TeamEventRegistration?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new ConfirmTeamRegistrationCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var (registration, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new ConfirmTeamRegistrationCommand(registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_Throws()
    {
        var (registration, _) = Setup();
        registration.Confirm();

        await Assert.ThrowsAsync<Domain.Registration.Exceptions.TeamRegistrationNotPendingException>(
            () => _handler.Handle(new ConfirmTeamRegistrationCommand(registration.Id.Value), default));
    }
}
