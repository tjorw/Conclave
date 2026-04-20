using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.CreateEvent;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class CreateEventHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly CreateEventHandler _handler;

    public CreateEventHandlerTests()
    {
        _handler = new CreateEventHandler(_eventRepo, _editionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person organiser,
             Domain.Convention.Aggregates.Edition edition, CategoryId categoryId) Setup()
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

        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _personRepo.GetByIdAsync(organiser.Id, Arg.Any<CancellationToken>()).Returns(organiser);

        return (convention, organiser, edition, category.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsEventId()
    {
        var (convention, organiser, edition, categoryId) = Setup();

        var id = await _handler.Handle(
            new CreateEventCommand(edition.Id.Value, categoryId.Value, organiser.Id.Value, convention.Id.Value), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var (convention, organiser, edition, categoryId) = Setup();

        await _handler.Handle(
            new CreateEventCommand(edition.Id.Value, categoryId.Value, organiser.Id.Value, convention.Id.Value), default);

        await _eventRepo.Received(1).AddAndSaveAsync(
            Arg.Any<Domain.Event.Aggregates.Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnpublishedEdition_Throws()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        var category = edition.CreateCategory("Rollspel", eventCoord.Id);

        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateEventCommand(edition.Id.Value, category.Id.Value, organiser.Id.Value, convention.Id.Value), default));
    }

    [Fact]
    public async Task Handle_UnknownCategory_Throws()
    {
        var (convention, organiser, edition, _) = Setup();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateEventCommand(edition.Id.Value, Guid.NewGuid(), organiser.Id.Value, convention.Id.Value), default));
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_Throws()
    {
        var (_, organiser, edition, categoryId) = Setup();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateEventCommand(edition.Id.Value, categoryId.Value, organiser.Id.Value, Guid.NewGuid()), default));
    }
}
