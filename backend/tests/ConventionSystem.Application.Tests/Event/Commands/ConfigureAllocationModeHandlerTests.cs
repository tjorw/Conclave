using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ConfigureAllocationMode;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public sealed class ConfigureAllocationModeHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ConfigureAllocationModeHandler _handler;

    public ConfigureAllocationModeHandlerTests()
    {
        _handler = new ConfigureAllocationModeHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
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

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, new CategoryId(Guid.NewGuid()), admin.Id);

        _currentUser.PersonId.Returns(admin.Id);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return ev;
    }

    [Fact]
    public async Task Handle_SetQueue_SetsAllocationMode()
    {
        var ev = Setup();

        await _handler.Handle(new ConfigureAllocationModeCommand(ev.Id.Value, "Queue"), default);

        Assert.Equal(AllocationMode.Queue, ev.AllocationMode);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetDirectConfirmation_SetsAllocationMode()
    {
        var ev = Setup();
        ev.ConfigureAllocationMode(AllocationMode.Queue);

        await _handler.Handle(new ConfigureAllocationModeCommand(ev.Id.Value, "DirectConfirmation"), default);

        Assert.Equal(AllocationMode.DirectConfirmation, ev.AllocationMode);
    }

    [Fact]
    public async Task Handle_InvalidMode_Throws()
    {
        var ev = Setup();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new ConfigureAllocationModeCommand(ev.Id.Value, "Ogiltigt"), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new ConfigureAllocationModeCommand(Guid.NewGuid(), "Queue"), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var ev = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new ConfigureAllocationModeCommand(ev.Id.Value, "Queue"), default));
    }
}
