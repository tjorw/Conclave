using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Event.Commands.ApproveCoOrganiserApplication;
using ConventionSystem.Application.Event.Commands.RejectCoOrganiserApplication;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Event.Commands;

public class ApproveCoOrganiserApplicationHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ApproveCoOrganiserApplicationHandler _approveHandler;
    private readonly RejectCoOrganiserApplicationHandler _rejectHandler;

    public ApproveCoOrganiserApplicationHandlerTests()
    {
        _approveHandler = new ApproveCoOrganiserApplicationHandler(
            _eventRepo, _editionRepo, _conventionRepo, _personRepo, _currentUser);
        _rejectHandler = new RejectCoOrganiserApplicationHandler(
            _eventRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person responsible,
             Domain.Convention.Entities.Person organiser,
             Domain.Convention.Aggregates.Edition edition,
             Domain.Event.Aggregates.Event ev,
             CoOrganiserApplicationId applicationId) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var responsible = convention.CreatePerson("Event", "event@example.com");
        var organiser = convention.CreatePerson("Arrangör", "organiser@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, responsible.Id);
        edition.Publish(admin.Id);
        var category = edition.CreateCategory("Rollspel", responsible.Id);

        var ev = new Domain.Event.Aggregates.Event(EventId.New(), edition.Id, category.Id, organiser.Id);
        var application = ev.SubmitCoOrganiserApplication("co@example.com", "Medarrangör", null, organiser.Id, organiser.Email);

        _eventRepo.GetByIdWithCoOrganisersAndApplicationsAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);
        _editionRepo.GetByIdWithCategoriesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(responsible.Id);

        return (convention, responsible, organiser, edition, ev, application.Id);
    }

    [Fact]
    public async Task Approve_ExistingPerson_AddsActiveCoOrganiser()
    {
        var (convention, _, _, _, ev, applicationId) = Setup();
        var coOrganiser = convention.CreatePerson("Medarrangör", "co@example.com");
        _personRepo.FindByEmailInConventionAsync(convention.Id, "co@example.com", Arg.Any<CancellationToken>())
            .Returns(coOrganiser);

        await _approveHandler.Handle(
            new ApproveCoOrganiserApplicationCommand(ev.Id.Value, applicationId.Value),
            default);

        Assert.Single(ev.CoOrganisers);
        Assert.Equal(coOrganiser.Id, ev.CoOrganisers[0].PersonId);
        Assert.Equal(CoOrganiserApplicationStatus.Approved, ev.CoOrganiserApplications[0].Status);
    }

    [Fact]
    public async Task Approve_MissingPerson_CreatesPerson()
    {
        var (convention, _, _, _, ev, applicationId) = Setup();
        _personRepo.FindByEmailInConventionAsync(convention.Id, "co@example.com", Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);

        await _approveHandler.Handle(
            new ApproveCoOrganiserApplicationCommand(ev.Id.Value, applicationId.Value),
            default);

        await _personRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Convention.Entities.Person>(p => p.Email == "co@example.com"),
            Arg.Any<CancellationToken>());
        Assert.Single(ev.CoOrganisers);
    }

    [Fact]
    public async Task Reject_DoesNotAddCoOrganiser()
    {
        var (_, _, _, _, ev, applicationId) = Setup();

        await _rejectHandler.Handle(
            new RejectCoOrganiserApplicationCommand(ev.Id.Value, applicationId.Value, "Inte aktuellt"),
            default);

        Assert.Empty(ev.CoOrganisers);
        Assert.Equal(CoOrganiserApplicationStatus.Rejected, ev.CoOrganiserApplications[0].Status);
        Assert.Equal("Inte aktuellt", ev.CoOrganiserApplications[0].ReviewComment);
    }
}
