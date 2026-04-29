using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AdjustCoOrganiserLimit;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class AdjustCoOrganiserLimitHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AdjustCoOrganiserLimitHandler _handler;

    public AdjustCoOrganiserLimitHandlerTests()
    {
        _handler = new AdjustCoOrganiserLimitHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev, Domain.Convention.Entities.Person admin) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "responsible@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, responsible.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", responsible.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, PersonId.New());
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(admin.Id);

        return (ev, admin);
    }

    [Fact]
    public async Task Handle_Admin_SetsLimit()
    {
        var (ev, _) = Setup();

        await _handler.Handle(new AdjustCoOrganiserLimitCommand(ev.Id.Value, 5), default);

        Assert.Equal(5, ev.CoOrganiserLimit);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotAdmin_ThrowsForbiddenException()
    {
        var (ev, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new AdjustCoOrganiserLimitCommand(ev.Id.Value, 5), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_ThrowsResourceNotFoundException()
    {
        Setup();
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>()).Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(new AdjustCoOrganiserLimitCommand(Guid.NewGuid(), 5), default));
    }
}
