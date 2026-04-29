using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.RedeemCoOrganiserInvitation;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class RedeemCoOrganiserInvitationHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly RedeemCoOrganiserInvitationHandler _handler;

    public RedeemCoOrganiserInvitationHandlerTests()
    {
        _handler = new RedeemCoOrganiserInvitationHandler(_eventRepo, _personRepo, _currentUser);
    }

    private (Domain.Event.Aggregates.Event ev,
             Domain.Convention.Entities.Person redeemer,
             Domain.Convention.Aggregates.Convention convention,
             string code) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var redeemer = convention.CreatePerson("Inbjuden", "redeemer@example.com");
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var responsible = convention.CreatePerson("Ansvarig", "responsible@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, responsible.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", responsible.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);
        ev.AdjustCoOrganiserLimit(3);
        var invitation = ev.CreateInvitation(redeemer.Email, organiser.Id);

        _eventRepo.GetByInvitationCodeAsync(invitation.Code, Arg.Any<CancellationToken>()).Returns(ev);
        _personRepo.GetByIdAsync(redeemer.Id, Arg.Any<CancellationToken>()).Returns(redeemer);
        _currentUser.PersonId.Returns(redeemer.Id);

        return (ev, redeemer, convention, invitation.Code);
    }

    [Fact]
    public async Task Handle_ValidCodeAndEmail_AddsCoOrganiser()
    {
        var (ev, _, _, code) = Setup();

        await _handler.Handle(new RedeemCoOrganiserInvitationCommand(code), default);

        Assert.Single(ev.CoOrganisers);
        Assert.Empty(ev.CoOrganiserInvitations);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidCode_ThrowsResourceNotFoundException()
    {
        Setup();
        _eventRepo.GetByInvitationCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Event.Aggregates.Event?) null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(new RedeemCoOrganiserInvitationCommand("invalid-code"), default));
    }

    [Fact]
    public async Task Handle_WrongEmail_ThrowsCoOrganiserInvitationEmailMismatchException()
    {
        var (ev, redeemer, convention, code) = Setup();
        var otherPerson = convention.CreatePerson("Annan", "other@example.com");
        _personRepo.GetByIdAsync(redeemer.Id, Arg.Any<CancellationToken>()).Returns(otherPerson);

        await Assert.ThrowsAsync<CoOrganiserInvitationEmailMismatchException>(() =>
            _handler.Handle(new RedeemCoOrganiserInvitationCommand(code), default));
    }
}
