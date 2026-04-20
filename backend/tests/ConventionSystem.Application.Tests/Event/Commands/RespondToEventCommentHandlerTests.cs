using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.RespondToEventComment;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class RespondToEventCommentHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RespondToEventCommentHandler _handler;

    public RespondToEventCommentHandlerTests()
    {
        _handler = new RespondToEventCommentHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev, Domain.Convention.Entities.Person categoryResponsible) Setup()
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
        var comment = ev.AddOrganiserComment(organiser.Id, "Önskar ändring.");

        _eventRepo.GetByIdWithCommentsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (ev, eventCoord);
    }

    [Fact]
    public async Task Handle_CategoryResponsible_Responds()
    {
        var (ev, categoryResponsible) = Setup();
        var commentId = ev.Comments[0].Id;
        _currentUser.PersonId.Returns(categoryResponsible.Id);

        await _handler.Handle(new RespondToEventCommentCommand(ev.Id.Value, commentId.Value, "Åtgärdat enligt önskemål."), default);

        Assert.Equal(EventCommentStatus.Responded, ev.Comments[0].Status);
        Assert.Equal("Åtgärdat enligt önskemål.", ev.Comments[0].HandlingComment);
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_Throws()
    {
        var (ev, _) = Setup();
        var commentId = ev.Comments[0].Id;
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RespondToEventCommentCommand(ev.Id.Value, commentId.Value, "Svar"), default));
    }
}
