using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ConfigureTeamRegistration;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public sealed class ConfigureTeamRegistrationHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ConfigureTeamRegistrationHandler _handler;

    public ConfigureTeamRegistrationHandlerTests()
    {
        _handler = new ConfigureTeamRegistrationHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private Domain.Event.Aggregates.Event Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@test.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@test.com");
        var eventCoord = convention.CreatePerson("Event", "event@test.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        var category = edition.CreateCategory("Rollspel", eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, PersonId.New());

        _currentUser.PersonId.Returns(admin.Id);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return ev;
    }

    [Fact]
    public async Task Handle_SetTeamMode_SetsRegistrationMode()
    {
        var ev = Setup();

        await _handler.Handle(new ConfigureTeamRegistrationCommand(ev.Id.Value, "Team", 2, 8), default);

        Assert.Equal(RegistrationMode.Team, ev.RegistrationMode);
        Assert.Equal(2, ev.TeamSize!.Min);
        Assert.Equal(8, ev.TeamSize!.Max);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetIndividualMode_ClearsTeamSize()
    {
        var ev = Setup();
        ev.ConfigureTeamRegistration(RegistrationMode.Team, 2, 8);

        await _handler.Handle(new ConfigureTeamRegistrationCommand(ev.Id.Value, "Individual", null, null), default);

        Assert.Equal(RegistrationMode.Individual, ev.RegistrationMode);
        Assert.Null(ev.TeamSize);
    }

    [Fact]
    public async Task Handle_InvalidMode_Throws()
    {
        var ev = Setup();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new ConfigureTeamRegistrationCommand(ev.Id.Value, "INVALID", 2, 4), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new ConfigureTeamRegistrationCommand(Guid.NewGuid(), "Team", 2, 4), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var ev = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new ConfigureTeamRegistrationCommand(ev.Id.Value, "Team", 2, 4), default));
    }
}
