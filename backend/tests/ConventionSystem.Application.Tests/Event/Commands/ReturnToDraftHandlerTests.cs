using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ReturnToDraft;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class ReturnToDraftHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ReturnToDraftHandler _handler;

    public ReturnToDraftHandlerTests()
    {
        _handler = new ReturnToDraftHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person responsible,
             Domain.Convention.Aggregates.Edition edition, Domain.Event.Aggregates.Event ev) Setup(EventStatus startStatus)
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
        ev.EditTitle("Rollspel");
        ev.EditDescription("Beskrivning");

        if (startStatus == EventStatus.UnderReview || startStatus == EventStatus.Published)
            ev.SubmitForReview();
        if (startStatus == EventStatus.Published)
            ev.Approve(eventCoord.Id);

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, eventCoord, edition, ev);
    }

    [Fact]
    public async Task Handle_FromUnderReview_StatusBecomesDraft()
    {
        var (_, responsible, _, ev) = Setup(EventStatus.UnderReview);
        _currentUser.PersonId.Returns(responsible.Id);

        await _handler.Handle(new ReturnToDraftCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.Draft, ev.Status);
    }

    [Fact]
    public async Task Handle_FromPublished_StatusBecomesDraft()
    {
        var (_, responsible, _, ev) = Setup(EventStatus.Published);
        _currentUser.PersonId.Returns(responsible.Id);

        await _handler.Handle(new ReturnToDraftCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.Draft, ev.Status);
    }

    [Fact]
    public async Task Handle_AlreadyDraft_Throws()
    {
        var (_, responsible, _, ev) = Setup(EventStatus.Draft);
        _currentUser.PersonId.Returns(responsible.Id);

        await Assert.ThrowsAsync<EventAlreadyDraftException>(
            () => _handler.Handle(new ReturnToDraftCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_FromCancelled_StatusBecomesDraft()
    {
        var (_, responsible, _, ev) = Setup(EventStatus.Draft);
        ev.CancelEvent(responsible.Id);
        _currentUser.PersonId.Returns(responsible.Id);

        await _handler.Handle(new ReturnToDraftCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.Draft, ev.Status);
    }

    [Fact]
    public async Task Handle_UnauthorisedPerson_Throws()
    {
        var (convention, _, _, ev) = Setup(EventStatus.UnderReview);
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(new ReturnToDraftCommand(ev.Id.Value), default));
    }
}
