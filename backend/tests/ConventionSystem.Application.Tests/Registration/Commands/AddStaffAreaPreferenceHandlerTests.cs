using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AddStaffAreaPreference;
using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AddStaffAreaPreferenceHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AddStaffAreaPreferenceHandler _handler;

    public AddStaffAreaPreferenceHandlerTests()
    {
        _handler = new AddStaffAreaPreferenceHandler(_applicationRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (StaffApplication application, Domain.Convention.Aggregates.Edition edition, StaffAreaId staffAreaId, Domain.Convention.Aggregates.Convention convention)
        Setup()
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

        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _editionRepo.GetByIdWithStructureAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(application.PersonId);

        return (application, edition, staffArea.Id, convention);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsPreferenceAndSaves()
    {
        var (application, _, staffAreaId, _) = Setup();

        await _handler.Handle(new AddStaffAreaPreferenceCommand(application.Id.Value, staffAreaId.Value), default);

        Assert.Single(application.StaffAreaPreferences);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StaffAreaNotOnEdition_Throws()
    {
        var (application, _, _, _) = Setup();
        var unknownStaffAreaId = StaffAreaId.New();

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(new AddStaffAreaPreferenceCommand(application.Id.Value, unknownStaffAreaId.Value), default));
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new AddStaffAreaPreferenceCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_Throws()
    {
        var (application, _, staffAreaId, convention) = Setup();
        _currentUser.PersonId.Returns(PersonId.New());
        _conventionRepo.GetByIdAsync(Arg.Any<ConventionId>(), Arg.Any<CancellationToken>()).Returns(convention);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new AddStaffAreaPreferenceCommand(application.Id.Value, staffAreaId.Value), default));
    }
}
