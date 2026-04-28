using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AddAvailability;
using ConventionSystem.Application.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AddAvailabilityHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AddAvailabilityHandler _handler;

    public AddAvailabilityHandlerTests()
    {
        _handler = new AddAvailabilityHandler(_applicationRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAvailabilityId()
    {
        var (application, edition, convention) = CreateApplicationContext();
        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(application.PersonId);

        var from = new DateTime(2027, 3, 1, 10, 0, 0);
        var to = new DateTime(2027, 3, 1, 18, 0, 0);

        var id = await _handler.Handle(new AddAvailabilityCommand(application.Id.Value, from, to), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsAvailabilityAndSaves()
    {
        var (application, edition, convention) = CreateApplicationContext();
        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(application.PersonId);

        var from = new DateTime(2027, 3, 1, 10, 0, 0);
        var to = new DateTime(2027, 3, 1, 18, 0, 0);

        await _handler.Handle(new AddAvailabilityCommand(application.Id.Value, from, to), default);

        Assert.Single(application.Availabilities);
        await _applicationRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdWithDetailsAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(
                new AddAvailabilityCommand(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(8)), default));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_Throws()
    {
        var (application, edition, convention) = CreateApplicationContext();
        _applicationRepo.GetByIdWithDetailsAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(PersonId.New());

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(
                new AddAvailabilityCommand(application.Id.Value, DateTime.UtcNow, DateTime.UtcNow.AddHours(1)),
                default));
    }

    private static (StaffApplication application, Domain.Convention.Aggregates.Edition edition, Domain.Convention.Aggregates.Convention convention)
        CreateApplicationContext()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);
        var applicant = convention.CreatePerson("Sokande", "applicant@example.com");
        var application = new StaffApplication(StaffApplicationId.New(), applicant.Id, edition.Id, "Intresserad");

        return (application, edition, convention);
    }
}
