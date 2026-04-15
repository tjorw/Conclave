using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AddEventComment;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class AddEventCommentHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AddEventCommentHandler _handler;

    public AddEventCommentHandlerTests()
    {
        _handler = new AddEventCommentHandler(_eventRepo, _currentUser);
    }

    private Domain.Event.Aggregates.Event SetupPublishedEvent()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test", "test");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Org", "org@example.com");
        var edition = convention.CreateEdition("Konvent", new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3)), staff.Id, eventCoord.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Kategori", eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);
        ev.EditTitle("Titel");
        ev.EditDescription("Beskrivning");
        ev.Approve(admin.Id);

        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _currentUser.PersonId.Returns(organiser.Id);
        return ev;
    }

    [Fact]
    public async Task Handle_OrganiserAddsComment_CommentAdded()
    {
        var ev = SetupPublishedEvent();

        await _handler.Handle(new AddEventCommentCommand(ev.Id.Value, "Kan vi flytta passet?"), default);

        Assert.Single(ev.Comments);
        Assert.Equal(EventCommentStatus.New, ev.Comments[0].Status);
    }

    [Fact]
    public async Task Handle_CoOrganiser_CanAddComment()
    {
        var ev = SetupPublishedEvent();
        var coOrganiserId = PersonId.New();
        ev.AddCoOrganiser(coOrganiserId);
        _currentUser.PersonId.Returns(coOrganiserId);

        await _handler.Handle(new AddEventCommentCommand(ev.Id.Value, "Kommentar från medarrangör."), default);

        Assert.Single(ev.Comments);
        Assert.Equal(EventCommentStatus.New, ev.Comments[0].Status);
    }

    [Fact]
    public async Task Handle_NonOrganiser_Throws()
    {
        var ev = SetupPublishedEvent();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new AddEventCommentCommand(ev.Id.Value, "Kommentar"), default));
    }
}
