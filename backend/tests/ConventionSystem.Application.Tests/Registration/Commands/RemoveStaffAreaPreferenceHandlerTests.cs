using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.RemoveStaffAreaPreference;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class RemoveStaffAreaPreferenceHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly RemoveStaffAreaPreferenceHandler _handler;

    public RemoveStaffAreaPreferenceHandlerTests()
    {
        _handler = new RemoveStaffAreaPreferenceHandler(_applicationRepo, _editionRepo, _conventionRepo);
    }

    private (StaffApplication application, StaffAreaId staffAreaId) SetupWithPreference()
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

        var applicant = convention.CreatePerson("Sökande", "applicant@example.com");
        var application = new StaffApplication(StaffApplicationId.New(), applicant.Id, edition.Id, "Intresserad");
        application.AddStaffAreaPreference(staffArea.Id);

        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (application, staffArea.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesPreferenceAndSaves()
    {
        var (application, staffAreaId) = SetupWithPreference();

        await _handler.Handle(new RemoveStaffAreaPreferenceCommand(application.Id.Value, staffAreaId.Value), default);

        Assert.Empty(application.StaffAreaPreferences);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new RemoveStaffAreaPreferenceCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
