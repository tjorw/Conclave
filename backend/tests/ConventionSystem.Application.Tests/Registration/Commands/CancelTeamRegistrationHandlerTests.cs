using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.CancelTeamRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public sealed class CancelTeamRegistrationHandlerTests
{
    private readonly ITeamEventRegistrationRepository _regRepo = Substitute.For<ITeamEventRegistrationRepository>();
    private readonly ITeamRepository _teamRepo = Substitute.For<ITeamRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelTeamRegistrationHandler _handler;

    public CancelTeamRegistrationHandlerTests()
    {
        _handler = new CancelTeamRegistrationHandler(_regRepo, _teamRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private record TestSetup(TeamEventRegistration Registration, Team Team,
        Domain.Convention.Aggregates.Convention Convention,
        Domain.Convention.Aggregates.Edition Edition,
        Domain.Convention.Entities.Person Admin,
        Domain.Convention.Entities.Person Captain);

    private TestSetup Setup(bool captainIsCurrent = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@test.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@test.com");
        var eventCoord = convention.CreatePerson("Event", "event@test.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);

        var captain = convention.CreatePerson("Kapten", "kapten@test.com");
        var team = new Team(TeamId.New(), edition.Id, captain.Id, "Lag Alpha");
        var registration = new TeamEventRegistration(
            TeamEventRegistrationId.New(), team.Id, EventId.New(), edition.Id);

        _regRepo.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _teamRepo.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _currentUser.PersonId.Returns(captainIsCurrent ? captain.Id : admin.Id);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return new TestSetup(registration, team, convention, edition, admin, captain);
    }

    [Fact]
    public async Task Handle_CaptainCancels_SetsCancelled()
    {
        var s = Setup(captainIsCurrent: true);

        await _handler.Handle(new CancelTeamRegistrationCommand(s.Registration.Id.Value), default);

        Assert.Equal(TeamRegistrationStatus.Cancelled, s.Registration.Status);
        await _regRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminCancels_SetsCancelled()
    {
        var s = Setup(captainIsCurrent: false);

        await _handler.Handle(new CancelTeamRegistrationCommand(s.Registration.Id.Value), default);

        Assert.Equal(TeamRegistrationStatus.Cancelled, s.Registration.Status);
    }

    [Fact]
    public async Task Handle_NonCaptainNonAdmin_Throws()
    {
        var s = Setup(captainIsCurrent: false);
        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Annan", "annan");
        var otherPerson = otherConvention.RegisterPerson("Annan", "annan@test.com");
        _currentUser.PersonId.Returns(otherPerson.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CancelTeamRegistrationCommand(s.Registration.Id.Value), default));
    }

    [Fact]
    public async Task Handle_RegistrationNotFound_Throws()
    {
        _regRepo.GetByIdAsync(Arg.Any<TeamEventRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((TeamEventRegistration?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new CancelTeamRegistrationCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_Throws()
    {
        var s = Setup(captainIsCurrent: true);
        s.Registration.Cancel(s.Captain.Id);

        await Assert.ThrowsAsync<Domain.Registration.Exceptions.TeamRegistrationAlreadyCancelledException>(
            () => _handler.Handle(new CancelTeamRegistrationCommand(s.Registration.Id.Value), default));
    }
}
