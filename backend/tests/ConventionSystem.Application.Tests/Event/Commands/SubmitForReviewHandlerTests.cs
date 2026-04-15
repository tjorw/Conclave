using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.SubmitForReview;
using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class SubmitForReviewHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SubmitForReviewHandler _handler;

    public SubmitForReviewHandlerTests()
    {
        _handler = new SubmitForReviewHandler(_eventRepo, _editionRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev, Domain.Convention.Aggregates.Edition edition) CreateReadyEvent(bool organiserRegOpen = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staffPerson = convention.CreatePerson("Staff", "staff@test.com");
        var evtPerson = convention.CreatePerson("Event", "event@test.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffPerson.Id, evtPerson.Id);
        edition.Publish(PersonId.New());
        if (organiserRegOpen) edition.OpenOrganiserRegistration(PersonId.New());

        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), edition.Id, CategoryId.New(), PersonId.New());
        ev.EditTitle("Rollspel för alla");
        ev.EditDescription("En spännande session.");
        ev.SetRegistrationType(RegistrationType.PreRegistration);

        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _currentUser.PersonId.Returns(ev.LeadOrganiserId);
        _currentUser.IsAdmin.Returns(false);

        return (ev, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_StatusBecomesUnderReview()
    {
        var (ev, _) = CreateReadyEvent();

        await _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.UnderReview, ev.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_RaisesSubmittedEvent()
    {
        var (ev, _) = CreateReadyEvent();
        ev.ClearDomainEvents();

        await _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default);

        Assert.Single(ev.DomainEvents.OfType<Domain.Event.Events.EventSubmittedForReview>());
    }

    [Fact]
    public async Task Handle_MissingTitle_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var staffPerson = convention.CreatePerson("Staff", "staff@test.com");
        var evtPerson = convention.CreatePerson("Event", "event@test.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffPerson.Id, evtPerson.Id);
        edition.Publish(PersonId.New());
        edition.OpenOrganiserRegistration(PersonId.New());

        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), edition.Id, CategoryId.New(), PersonId.New());
        ev.EditDescription("Beskrivning");
        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _currentUser.PersonId.Returns(ev.LeadOrganiserId);
        _currentUser.IsAdmin.Returns(false);

        await Assert.ThrowsAsync<EventTitleRequiredException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyUnderReview_Throws()
    {
        var (ev, _) = CreateReadyEvent();
        ev.SubmitForReview();
        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        await Assert.ThrowsAsync<EventAlreadyUnderReviewException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NotOrganiser_Throws()
    {
        var (ev, _) = CreateReadyEvent();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_OrganiserRegistrationClosed_NonAdmin_Throws()
    {
        var (ev, _) = CreateReadyEvent(organiserRegOpen: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default));
    }

    [Fact]
    public async Task Handle_OrganiserRegistrationClosed_Admin_Succeeds()
    {
        var (ev, _) = CreateReadyEvent(organiserRegOpen: false);
        _currentUser.IsAdmin.Returns(true);

        await _handler.Handle(new SubmitForReviewCommand(ev.Id.Value), default);

        Assert.Equal(EventStatus.UnderReview, ev.Status);
    }
}
