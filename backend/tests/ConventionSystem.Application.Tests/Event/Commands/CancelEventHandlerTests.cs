using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.CancelEvent;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class CancelEventHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelEventHandler _handler;

    public CancelEventHandlerTests()
    {
        _handler = new CancelEventHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition, Domain.Event.Aggregates.Event ev) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, eventCoord, edition, ev);
    }

    [Fact]
    public async Task Handle_CategoryResponsible_EventBecomesCancelled()
    {
        var (_, responsible, _, ev) = Setup();
        _currentUser.PersonId.Returns(responsible.Id);

        await _handler.Handle(new CancelEventCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.Cancelled, ev.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_RaisesEventCancelledEvent()
    {
        var (_, responsible, _, ev) = Setup();
        ev.ClearDomainEvents();
        _currentUser.PersonId.Returns(responsible.Id);

        await _handler.Handle(new CancelEventCommand(ev.Id.Value), default);

        Assert.Single(ev.DomainEvents.OfType<EventCancelled>());
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_Throws()
    {
        var (_, responsible, _, ev) = Setup();
        ev.CancelEvent(responsible.Id);
        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _currentUser.PersonId.Returns(responsible.Id);

        await Assert.ThrowsAsync<EventAlreadyCancelledException>(
            () => _handler.Handle(new CancelEventCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_UnauthorisedPerson_Throws()
    {
        var (convention, _, _, ev) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new CancelEventCommand(ev.Id.Value), default));
    }
}
