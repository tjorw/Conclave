using ConventionSystem.Application.Common;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.CancelCoOrganiserApplication;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Exceptions;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class CancelCoOrganiserApplicationHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelCoOrganiserApplicationHandler _handler;

    public CancelCoOrganiserApplicationHandlerTests()
    {
        _handler = new CancelCoOrganiserApplicationHandler(_eventRepo, _currentUser);
    }

    private (Domain.Convention.Entities.Person organiser,
             Domain.Event.Aggregates.Event ev,
             CoOrganiserApplicationId applicationId) Setup()
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
        var application = ev.SubmitCoOrganiserApplication("co@example.com", null, null, organiser.Id, organiser.Email);

        _eventRepo.GetByIdWithCoOrganisersAndApplicationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _currentUser.PersonId.Returns(organiser.Id);

        return (organiser, ev, application.Id);
    }

    [Fact]
    public async Task Handle_LeadOrganiser_CancelsPendingApplication()
    {
        var (_, ev, applicationId) = Setup();

        await _handler.Handle(new CancelCoOrganiserApplicationCommand(ev.Id.Value, applicationId.Value), default);

        Assert.Equal(CoOrganiserApplicationStatus.Cancelled, ev.CoOrganiserApplications[0].Status);
        await _eventRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonLeadOrganiser_Throws()
    {
        var (_, ev, applicationId) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new CancelCoOrganiserApplicationCommand(ev.Id.Value, applicationId.Value), default));
    }

    [Fact]
    public async Task Handle_AlreadyReviewedApplication_Throws()
    {
        var (organiser, ev, applicationId) = Setup();
        ev.CancelCoOrganiserApplication(applicationId, organiser.Id);

        await Assert.ThrowsAsync<CoOrganiserApplicationAlreadyReviewedException>(() =>
            _handler.Handle(new CancelCoOrganiserApplicationCommand(ev.Id.Value, applicationId.Value), default));
    }
}
