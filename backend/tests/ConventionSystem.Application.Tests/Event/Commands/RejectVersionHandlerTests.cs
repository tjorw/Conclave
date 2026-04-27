using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.RejectVersion;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Events;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class RejectVersionHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RejectVersionHandler _handler;

    public RejectVersionHandlerTests()
    {
        _handler = new RejectVersionHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person admin,
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
        ev.EditTitle("Rollspel");
        ev.EditDescription("Beskrivning");
        ev.SubmitForReview();

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, ev);
    }

    [Fact]
    public async Task Handle_ValidCommand_EventReturnsToDraft()
    {
        var (_, admin, _, ev) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RejectVersionCommand(ev.Id.Value, "Behöver förbättras."), default);

        Assert.Equal(EventStatus.Draft, ev.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_CommentIsAdded()
    {
        var (_, admin, _, ev) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RejectVersionCommand(ev.Id.Value, "Behöver förbättras."), default);

        Assert.Single(ev.Comments);
        Assert.Equal("Behöver förbättras.", ev.Comments[0].Text);
    }

    [Fact]
    public async Task Handle_ValidCommand_RaisesEventRejectedEvent()
    {
        var (_, admin, _, ev) = Setup();
        ev.ClearDomainEvents();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new RejectVersionCommand(ev.Id.Value, "Kommentar."), default);

        Assert.Single(ev.DomainEvents.OfType<EventRejected>());
    }

    [Fact]
    public async Task Handle_EmptyComment_Throws()
    {
        var (_, admin, _, ev) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new RejectVersionCommand(ev.Id.Value, "  "), default));
    }

    [Fact]
    public async Task Handle_UnauthorisedPerson_Throws()
    {
        var (convention, _, _, ev) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "other@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new RejectVersionCommand(ev.Id.Value, "Kommentar."), default));
    }
}
