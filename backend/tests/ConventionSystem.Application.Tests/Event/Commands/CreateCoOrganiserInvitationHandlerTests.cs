using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.CreateCoOrganiserInvitation;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class CreateCoOrganiserInvitationHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreateCoOrganiserInvitationHandler _handler;

    public CreateCoOrganiserInvitationHandlerTests()
    {
        _handler = new CreateCoOrganiserInvitationHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev,
             Domain.Convention.Entities.Person admin,
             PersonId leadId) Setup(int limit = 3)
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
        ev.AdjustCoOrganiserLimit(limit);

        _eventRepo.GetByIdWithCoOrganisersAndInvitationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(organiser.Id);

        return (ev, admin, organiser.Id);
    }

    [Fact]
    public async Task Handle_LeadOrganiser_CreatesInvitation()
    {
        var (ev, _, _) = Setup();

        await _handler.Handle(new CreateCoOrganiserInvitationCommand(ev.Id.Value, "invite@example.com"), default);

        Assert.Single(ev.CoOrganiserInvitations);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Admin_CreatesInvitation()
    {
        var (ev, admin, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CreateCoOrganiserInvitationCommand(ev.Id.Value, "invite@example.com"), default);

        Assert.Single(ev.CoOrganiserInvitations);
    }

    [Fact]
    public async Task Handle_NotOrganiserOrAdmin_ThrowsForbiddenException()
    {
        var (ev, _, _) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new CreateCoOrganiserInvitationCommand(ev.Id.Value, "invite@example.com"), default));
    }

    [Fact]
    public async Task Handle_LimitZero_ThrowsCoOrganiserLimitExceededException()
    {
        var (ev, _, _) = Setup(limit: 0);

        await Assert.ThrowsAsync<CoOrganiserLimitExceededException>(() =>
            _handler.Handle(new CreateCoOrganiserInvitationCommand(ev.Id.Value, "invite@example.com"), default));
    }
}
