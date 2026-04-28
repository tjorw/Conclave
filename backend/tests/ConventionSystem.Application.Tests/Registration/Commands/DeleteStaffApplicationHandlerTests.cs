using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.DeleteStaffApplication;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class DeleteStaffApplicationHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly DeleteStaffApplicationHandler _handler;

    public DeleteStaffApplicationHandlerTests()
    {
        _handler = new DeleteStaffApplicationHandler(_applicationRepo, _editionRepo, _conventionRepo, _currentUser);
    }

    [Fact]
    public async Task Handle_AdminDeletesApplication_CallsRepositoryDelete()
    {
        var (_, admin, _, application) = Setup();
        _currentUser.PersonId.Returns(admin.Id);

        await _handler.Handle(new DeleteStaffApplicationCommand(application.Id.Value), default);

        await _applicationRepo.Received(1).DeleteAsync(application, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFound_Throws()
    {
        _applicationRepo.GetByIdAsync(Arg.Any<StaffApplicationId>(), Arg.Any<CancellationToken>())
            .Returns((StaffApplication?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _handler.Handle(new DeleteStaffApplicationCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NonAdmin_Throws()
    {
        var (convention, _, _, application) = Setup();
        var outsider = convention.CreatePerson("Outsider", "outsider@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new DeleteStaffApplicationCommand(application.Id.Value), default));
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

        var applicant = convention.CreatePerson("Sokande", "applicant@example.com");
        var application = new StaffApplication(StaffApplicationId.New(), applicant.Id, edition.Id, "Intresserad");

        _applicationRepo.GetByIdAsync(application.Id, Arg.Any<CancellationToken>()).Returns(application);
        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, edition, application);
    }
}
