using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.AddCoOrganiser;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class AddCoOrganiserHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AddCoOrganiserHandler _handler;

    public AddCoOrganiserHandlerTests()
    {
        _handler = new AddCoOrganiserHandler(_eventRepo, _personRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person organiser,
             Domain.Event.Aggregates.Event ev) Setup()
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
        _eventRepo.GetByIdWithCoOrganisersAndApplicationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _personRepo.GetByIdAsync(organiser.Id, Arg.Any<CancellationToken>()).Returns(organiser);
        _currentUser.PersonId.Returns(organiser.Id);

        return (convention, organiser, ev);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPendingApplication()
    {
        var (convention, _, ev) = Setup();

        await _handler.Handle(new AddCoOrganiserCommand(
            ev.Id.Value,
            "co@example.com",
            "Medarrangör",
            "Hjälper till",
            convention.Id.Value), default);

        var application = Assert.Single(ev.CoOrganiserApplications);
        Assert.Equal(CoOrganiserApplicationStatus.Pending, application.Status);
        Assert.Equal("CO@EXAMPLE.COM", application.NormalizedEmail);
        Assert.Empty(ev.CoOrganisers);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (convention, _, ev) = Setup();

        await _handler.Handle(new AddCoOrganiserCommand(
            ev.Id.Value,
            "co@example.com",
            null,
            null,
            convention.Id.Value), default);

        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicatePendingEmail_Throws()
    {
        var (convention, organiser, ev) = Setup();
        ev.SubmitCoOrganiserApplication("co@example.com", null, null, organiser.Id, "organiser@example.com");

        await Assert.ThrowsAsync<CoOrganiserApplicationAlreadyPendingException>(() =>
            _handler.Handle(new AddCoOrganiserCommand(
                ev.Id.Value,
                "CO@example.com",
                null,
                null,
                convention.Id.Value), default));
    }

    [Fact]
    public async Task Handle_EmailForActiveCoOrganiser_Throws()
    {
        var (convention, _, ev) = Setup();
        var coOrganiser = convention.CreatePerson("Medarrangör", "co@example.com");
        ev.AddCoOrganiser(coOrganiser.Id);
        _personRepo.FindByEmailInConventionAsync(convention.Id, "co@example.com", Arg.Any<CancellationToken>())
            .Returns(coOrganiser);

        await Assert.ThrowsAsync<CoOrganiserAlreadyAddedException>(() =>
            _handler.Handle(new AddCoOrganiserCommand(
                ev.Id.Value,
                "co@example.com",
                null,
                null,
                convention.Id.Value), default));
    }

    [Fact]
    public async Task Handle_LeadOrganiserEmail_Throws()
    {
        var (convention, _, ev) = Setup();

        await Assert.ThrowsAsync<LeadOrganiserCannotBeCoOrganiserException>(() =>
            _handler.Handle(new AddCoOrganiserCommand(
                ev.Id.Value,
                "organiser@example.com",
                null,
                null,
                convention.Id.Value), default));
    }

    [Fact]
    public async Task Handle_NonLeadOrganiser_Throws()
    {
        var (convention, _, ev) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new AddCoOrganiserCommand(
                ev.Id.Value,
                "co@example.com",
                null,
                null,
                convention.Id.Value), default));
    }
}
