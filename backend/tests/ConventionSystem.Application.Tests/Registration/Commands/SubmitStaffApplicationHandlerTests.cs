using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.SubmitStaffApplication;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class SubmitStaffApplicationHandlerTests
{
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly SubmitStaffApplicationHandler _handler;

    public SubmitStaffApplicationHandlerTests()
    {
        _handler = new SubmitStaffApplicationHandler(_applicationRepo, _editionRepo, _personRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person person,
             Domain.Convention.Aggregates.Edition edition) Setup(bool staffRegOpen = true)
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var person = convention.CreatePerson("Sökande", "applicant@example.com");
        var period = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition = convention.CreateEdition("Konvent 2027", period, staff.Id, evt.Id);

        edition.Publish(admin.Id);
        if (staffRegOpen)
            edition.OpenStaffRegistration(admin.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _personRepo.GetByIdAsync(person.Id, Arg.Any<CancellationToken>()).Returns(person);
        _applicationRepo.HasActiveApplicationAsync(person.Id, edition.Id, Arg.Any<CancellationToken>()).Returns(false);

        return (convention, person, edition);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsApplicationId()
    {
        var (_, person, edition) = Setup();

        var id = await _handler.Handle(new SubmitStaffApplicationCommand(edition.Id.Value, person.Id.Value, "Jag vill jobba i receptionen"), default);

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        var (_, person, edition) = Setup();

        await _handler.Handle(new SubmitStaffApplicationCommand(edition.Id.Value, person.Id.Value, "Intresserad"), default);

        await _applicationRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Registration.Aggregates.StaffApplication>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StaffRegistrationNotOpen_Throws()
    {
        var (_, person, edition) = Setup(staffRegOpen: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitStaffApplicationCommand(edition.Id.Value, person.Id.Value, "Intresserad"), default));
    }

    [Fact]
    public async Task Handle_DuplicateApplication_Throws()
    {
        var (_, person, edition) = Setup();
        _applicationRepo.HasActiveApplicationAsync(person.Id, edition.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitStaffApplicationCommand(edition.Id.Value, person.Id.Value, "Intresserad"), default));
    }

    [Fact]
    public async Task Handle_InactivePerson_Throws()
    {
        var (convention, person, edition) = Setup();
        convention.DeactivatePerson(person);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new SubmitStaffApplicationCommand(edition.Id.Value, person.Id.Value, "Intresserad"), default));
    }
}
