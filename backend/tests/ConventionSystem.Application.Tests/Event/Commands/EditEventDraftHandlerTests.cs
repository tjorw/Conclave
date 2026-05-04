using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.EditEventDraft;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class EditEventDraftHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly EditEventDraftHandler _handler;

    public EditEventDraftHandlerTests()
    {
        _handler = new EditEventDraftHandler(_eventRepo, _editionRepo);
    }

    private (Domain.Event.Aggregates.Event Event, Domain.Convention.Aggregates.Edition Edition) CreateDraftEvent()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        edition.AddProgramTagDefinition("Barnvänligt");
        edition.AddProgramTagDefinition("Nybörjare");

        var ev = new Domain.Event.Aggregates.Event(
            EventId.New(), edition.Id, CategoryId.New(), PersonId.New());

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithProgramTagDefinitionsAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        return (ev, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesEvent()
    {
        var (ev, _) = CreateDraftEvent();

        await _handler.Handle(
            new EditEventDraftCommand(ev.Id.Value, "Rollspel för nybörjare",
                "En beskrivning", ["Barnvänligt"], RegistrationType.PreRegistration, null, "Helst fredag kväll.", 2), default);

        Assert.Equal("Rollspel för nybörjare", ev.Title);
        Assert.Equal("En beskrivning", ev.Description);
        Assert.Equal(["Barnvänligt"], ev.ProgramTags.Select(t => t.Name));
        Assert.Equal("Helst fredag kväll.", ev.ScheduleRequestText);
        Assert.Equal(RegistrationType.PreRegistration, ev.RegistrationType);
        Assert.Equal(2, ev.CoOrganiserCount);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (ev, _) = CreateDraftEvent();

        await _handler.Handle(
            new EditEventDraftCommand(ev.Id.Value, "Titel", "Beskrivning", [], RegistrationType.DropIn, "Öppen dörrpolicy", null, 0), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyTitle_Throws()
    {
        var (ev, _) = CreateDraftEvent();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new EditEventDraftCommand(ev.Id.Value, "  ", "Beskrivning", [], RegistrationType.PreRegistration, null, null, 0), default));
    }

    [Fact]
    public async Task Handle_EventNotFound_Throws()
    {
        _eventRepo.GetByIdAsync(Arg.Any<EventId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(
                new EditEventDraftCommand(Guid.NewGuid(), "Titel", "Beskrivning", [], RegistrationType.PreRegistration, null, null, 0), default));
    }

    [Fact]
    public async Task Handle_CancelledEvent_Throws()
    {
        var (ev, _) = CreateDraftEvent();
        ev.CancelEvent(PersonId.New());

        await Assert.ThrowsAsync<EventIsCancelledAndReadOnlyException>(() =>
            _handler.Handle(
                new EditEventDraftCommand(ev.Id.Value, "Titel", "Beskrivning", [], RegistrationType.PreRegistration, null, null, 0), default));
    }

    [Fact]
    public async Task Handle_UnknownProgramTag_Throws()
    {
        var (ev, _) = CreateDraftEvent();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new EditEventDraftCommand(ev.Id.Value, "Titel", "Beskrivning", ["Okänd"], RegistrationType.PreRegistration, null, null, 0), default));
    }
}
