using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AcceptStaffApplication;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AcceptStaffApplicationHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly AcceptStaffApplicationHandler _handler;

    public AcceptStaffApplicationHandlerTests()
    {
        _handler = new AcceptStaffApplicationHandler(_applicationRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition,
             StaffApplication application) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evt.Id);
        edition.Publish(admin.Id);
        edition.OpenStaffRegistration(admin.Id);

        var applicant = convention.CreatePerson("Sökande", "applicant@example.com");
        var application = new StaffApplication(StaffApplicationId.New(), applicant.Id, edition.Id, "Intresserad");

        _applicationRepo.GetByIdAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, application);
    }

    [Fact]
    public async Task Handle_AdminAccepts_TransitionsToConfirmed()
    {
        var (_, admin, _, application) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new AcceptStaffApplicationCommand(application.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.StaffApplicationStatus.Confirmed, application.Status);
    }

    [Fact]
    public async Task Handle_StaffCoordinatorAccepts_TransitionsToConfirmed()
    {
        var (_, _, edition, application) = Setup();
        _currentUser.PersonId.Returns(edition.StaffCoordinatorId!.Value);

        await _handler.Handle(new AcceptStaffApplicationCommand(application.Id.Value), default);

        Assert.Equal(Domain.Registration.Enums.StaffApplicationStatus.Confirmed, application.Status);
    }

    [Fact]
    public async Task Handle_UnauthorizedPerson_Throws()
    {
        var (convention, _, _, application) = Setup();
        var nonAdmin = convention.CreatePerson("Annan", "annan@example.com");
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new AcceptStaffApplicationCommand(application.Id.Value), default));
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new AcceptStaffApplicationCommand(Guid.NewGuid()), default));
    }
}
