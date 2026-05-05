using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.SetFeatured;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class SetFeaturedHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly SetFeaturedHandler _handler;

    public SetFeaturedHandlerTests()
    {
        _handler = new SetFeaturedHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Event.Aggregates.Event ev) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var eventCoord = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, eventCoord.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, CategoryId.New(), admin.Id);

        _eventRepo.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(admin.Id);

        return (convention, admin, edition, ev);
    }

    [Fact]
    public async Task Handle_EnableFeatured_SetsFlagAndSaves()
    {
        var (_, _, edition, ev) = Setup();
        _eventRepo.CountFeaturedByEditionIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(3);

        await _handler.Handle(new SetFeaturedCommand(ev.Id.Value, true, 1), default);

        Assert.True(ev.IsFeatured);
        Assert.Equal(1, ev.FeaturedSortOrder);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeventhFeaturedEvent_Throws()
    {
        var (_, _, edition, ev) = Setup();
        _eventRepo.CountFeaturedByEditionIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(6);

        await Assert.ThrowsAsync<EventFeaturedLimitExceededException>(() =>
            _handler.Handle(new SetFeaturedCommand(ev.Id.Value, true, 0), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_ThrowsForbidden()
    {
        var (convention, _, _, ev) = Setup();
        var outsider = convention.CreatePerson("Utomstående", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new SetFeaturedCommand(ev.Id.Value, true, 0), default));
    }
}
