using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.CancelCoOrganiserInvitation;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class CancelCoOrganiserInvitationHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelCoOrganiserInvitationHandler _handler;

    public CancelCoOrganiserInvitationHandlerTests()
    {
        _handler = new CancelCoOrganiserInvitationHandler(_eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev,
             Domain.Convention.Entities.Person admin,
             PersonId leadId) Setup()
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
        ev.AdjustCoOrganiserLimit(3);

        _eventRepo.GetByIdWithInvitationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(organiser.Id);

        return (ev, admin, organiser.Id);
    }

    [Fact]
    public async Task Handle_LeadOrganiser_CancelsInvitation()
    {
        var (ev, _, _) = Setup();
        var invitation = ev.CreateInvitation("invite@example.com", PersonId.New());

        await _handler.Handle(new CancelCoOrganiserInvitationCommand(ev.Id.Value, invitation.Id.Value), default);

        Assert.Equal(CoOrganiserInvitationStatus.Cancelled, invitation.Status);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Admin_CancelsInvitation()
    {
        var (ev, admin, _) = Setup();
        var invitation = ev.CreateInvitation("invite@example.com", PersonId.New());
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new CancelCoOrganiserInvitationCommand(ev.Id.Value, invitation.Id.Value), default);

        Assert.Equal(CoOrganiserInvitationStatus.Cancelled, invitation.Status);
    }

    [Fact]
    public async Task Handle_NotOrganiserOrAdmin_ThrowsForbiddenException()
    {
        var (ev, _, _) = Setup();
        var invitation = ev.CreateInvitation("invite@example.com", PersonId.New());
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new CancelCoOrganiserInvitationCommand(ev.Id.Value, invitation.Id.Value), default));
    }
}
