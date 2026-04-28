using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.UpdateStaffApplication;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class UpdateStaffApplicationHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateStaffApplicationHandler _handler;

    public UpdateStaffApplicationHandlerTests()
    {
        _handler = new UpdateStaffApplicationHandler(_applicationRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_AdminUpdatesApplication_UpdatesFieldsAndSaves()
    {
        var (convention, admin, edition, application, staffArea) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new UpdateStaffApplicationCommand(
            application.Id.Value,
            "Kan jobba i reception och garderob",
            [new UpdateStaffApplicationAvailability(
                new DateTime(2027, 3, 2, 8, 0, 0),
                new DateTime(2027, 3, 2, 12, 0, 0))],
            [staffArea.Id.Value]), default);

        Assert.Equal("Kan jobba i reception och garderob", application.InterestDescription);
        Assert.Single(application.Availabilities);
        Assert.Single(application.StaffAreaPreferences);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(new UpdateStaffApplicationCommand(Guid.NewGuid(), "Test", [], []), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var (convention, _, _, application, staffArea) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new UpdateStaffApplicationCommand(
                application.Id.Value,
                "Ny text",
                [],
                [staffArea.Id.Value]), default));
    }

    [Fact]
    public async Task Handle_UnknownStaffArea_Throws()
    {
        var (_, admin, _, application, _) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await Assert.ThrowsAsync<DomainRuleViolationException>(() =>
            _handler.Handle(new UpdateStaffApplicationCommand(
                application.Id.Value,
                "Ny text",
                [],
                [Guid.NewGuid()]), default));
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             StaffApplication application,
             Domain.Convention.Entities.StaffArea staffArea) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);
        var staffArea = edition.CreateStaffArea("Reception", admin.Id);
        edition.CreateStation("Info-disk", staffArea.Id);

        var applicant = convention.CreatePerson("Sokande", "applicant@example.com");
        var application = new StaffApplication(StaffApplicationId.New(), applicant.Id, edition.Id, "Intresserad");
        application.AddAvailability(new DateTime(2027, 3, 1, 10, 0, 0), new DateTime(2027, 3, 1, 18, 0, 0));
        application.AddStaffAreaPreference(staffArea.Id);

        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _editionRepo.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, application, staffArea);
    }
}
