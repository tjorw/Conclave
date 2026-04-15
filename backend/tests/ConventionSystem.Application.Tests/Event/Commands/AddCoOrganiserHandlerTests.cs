using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AddCoOrganiser;
using ConventionSystem.Domain.Convention.Aggregates;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class AddCoOrganiserHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly AddCoOrganiserHandler _handler;

    public AddCoOrganiserHandlerTests()
    {
        _handler = new AddCoOrganiserHandler(_eventRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention, Domain.Convention.Entities.Person coOrganiser,
             Domain.Event.Aggregates.Event ev) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var coOrganiser = convention.CreatePerson("Medarrangör", "co@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, eventCoord.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);
        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _personRepo.GetByIdAsync(coOrganiser.Id, Arg.Any<CancellationToken>()).Returns(coOrganiser);

        return (convention, coOrganiser, ev);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCoOrganiser()
    {
        var (convention, coOrganiser, ev) = Setup();

        await _handler.Handle(new AddCoOrganiserCommand(ev.Id.Value, coOrganiser.Id.Value, convention.Id.Value), default);

        Assert.Single(ev.CoOrganisers);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (convention, coOrganiser, ev) = Setup();

        await _handler.Handle(new AddCoOrganiserCommand(ev.Id.Value, coOrganiser.Id.Value, convention.Id.Value), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCoOrganiser_Throws()
    {
        var (convention, coOrganiser, ev) = Setup();
        ev.AddCoOrganiser(coOrganiser.Id);

        await Assert.ThrowsAsync<CoOrganiserAlreadyAddedException>(() =>
            _handler.Handle(new AddCoOrganiserCommand(ev.Id.Value, coOrganiser.Id.Value, convention.Id.Value), default));
    }

    [Fact]
    public async Task Handle_PersonFromOtherConvention_Throws()
    {
        var (_, coOrganiser, ev) = Setup();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new AddCoOrganiserCommand(ev.Id.Value, coOrganiser.Id.Value, Guid.NewGuid()), default));
    }
}
