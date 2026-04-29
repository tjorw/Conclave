using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.RemoveCoOrganiser;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class RemoveCoOrganiserHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RemoveCoOrganiserHandler _handler;

    public RemoveCoOrganiserHandlerTests()
    {
        _handler = new RemoveCoOrganiserHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Event.Aggregates.Event ev,
             PersonId coOrganiserId) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "responsible@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, responsible.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", responsible.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);
        var coOrganiser = convention.CreatePerson("Medarrangör", "co@example.com");
        ev.AddCoOrganiser(coOrganiser.Id);

        _eventRepo.GetByIdWithCoOrganisersAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(admin.Id);

        return (convention, admin, edition, ev, coOrganiser.Id);
    }

    [Fact]
    public async Task Remove_ExistingCoOrganiser_RemovesFromEvent()
    {
        var (_, _, _, ev, coOrganiserId) = Setup();

        await _handler.Handle(
            new RemoveCoOrganiserCommand(ev.Id.Value, coOrganiserId.Value),
            default);

        Assert.Empty(ev.CoOrganisers);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_NonExistingCoOrganiser_ThrowsCoOrganiserNotFoundException()
    {
        var (_, _, _, ev, _) = Setup();

        await Assert.ThrowsAsync<CoOrganiserNotFoundException>(() =>
            _handler.Handle(
                new RemoveCoOrganiserCommand(ev.Id.Value, Guid.NewGuid()),
                default));
    }
}
