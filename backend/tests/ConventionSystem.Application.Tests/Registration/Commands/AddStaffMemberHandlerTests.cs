using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Registration.Commands.AddStaffMember;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Registration.Enums;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Registration.Commands;

public class AddStaffMemberHandlerTests
{
    private readonly IEditionRepository _editionRepo             = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo       = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo               = Substitute.For<IPersonRepository>();
    private readonly IStaffApplicationRepository _applicationRepo = Substitute.For<IStaffApplicationRepository>();
    private readonly ICurrentUser _currentUser                   = Substitute.For<ICurrentUser>();
    private readonly AddStaffMemberHandler _handler;

    public AddStaffMemberHandlerTests()
    {
        _handler = new AddStaffMemberHandler(
            _editionRepo, _conventionRepo, _personRepo, _applicationRepo, _currentUser);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition edition) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin      = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);
        var staffCoord = convention.CreatePerson("Staff", "staff@example.com");
        var evtCoord   = convention.CreatePerson("Event", "event@example.com");
        var period     = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var edition    = convention.CreateEdition("Konvent 2027", period, staffCoord.Id, evtCoord.Id);
        edition.Publish(admin.Id);

        _editionRepo.GetByIdAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _currentUser.PersonId.Returns(admin.Id);
        _applicationRepo.HasActiveApplicationAsync(Arg.Any<Domain.Convention.Ids.PersonId>(), edition.Id, Arg.Any<CancellationToken>()).Returns(false);

        return (convention, admin, edition);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesPersonAndConfirmedApplication()
    {
        var (_, _, edition) = Setup();
        _personRepo.FindByEmailInConventionAsync(Arg.Any<ConventionId>(), "ny@example.com", Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);

        var id = await _handler.Handle(
            new AddStaffMemberCommand(edition.Id.Value, "Ny Person", "ny@example.com", null, null), default);

        Assert.NotEqual(Guid.Empty, id);
        await _personRepo.Received(1).AddAndSaveAsync(Arg.Any<Domain.Convention.Entities.Person>(), Arg.Any<CancellationToken>());
        await _applicationRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Registration.Aggregates.StaffApplication>(a => a.Status == StaffApplicationStatus.Confirmed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingEmail_ReusesPersonWithoutCreating()
    {
        var (convention, _, edition) = Setup();
        var existing = convention.CreatePerson("Befintlig", "befintlig@example.com");
        _personRepo.FindByEmailInConventionAsync(Arg.Any<ConventionId>(), "befintlig@example.com", Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.Handle(
            new AddStaffMemberCommand(edition.Id.Value, "Ignorerat Namn", "befintlig@example.com", null, null), default);

        await _personRepo.DidNotReceive().AddAndSaveAsync(Arg.Any<Domain.Convention.Entities.Person>(), Arg.Any<CancellationToken>());
        await _applicationRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Registration.Aggregates.StaffApplication>(a => a.PersonId == existing.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewEmailWithoutName_Throws()
    {
        var (_, _, edition) = Setup();
        _personRepo.FindByEmailInConventionAsync(Arg.Any<ConventionId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Entities.Person?)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(
                new AddStaffMemberCommand(edition.Id.Value, "", "ny@example.com", null, null), default));
    }

    [Fact]
    public async Task Handle_DuplicateActiveApplication_Throws()
    {
        var (convention, _, edition) = Setup();
        var existing = convention.CreatePerson("Person", "person@example.com");
        _personRepo.FindByEmailInConventionAsync(Arg.Any<ConventionId>(), "person@example.com", Arg.Any<CancellationToken>())
            .Returns(existing);
        _applicationRepo.HasActiveApplicationAsync(existing.Id, edition.Id, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => _handler.Handle(
                new AddStaffMemberCommand(edition.Id.Value, "", "person@example.com", null, null), default));
    }

    [Fact]
    public async Task Handle_UnauthorizedCaller_Throws()
    {
        var (convention, _, edition) = Setup();
        var outsider = convention.CreatePerson("Annan", "annan@example.com");
        _currentUser.PersonId.Returns(outsider.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new AddStaffMemberCommand(edition.Id.Value, "X", "x@example.com", null, null), default));
    }

    [Fact]
    public async Task Handle_WithNote_UsesNoteAsDescription()
    {
        var (convention, _, edition) = Setup();
        var existing = convention.CreatePerson("Person", "person@example.com");
        _personRepo.FindByEmailInConventionAsync(Arg.Any<ConventionId>(), "person@example.com", Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.Handle(
            new AddStaffMemberCommand(edition.Id.Value, "", "person@example.com", null, "Rekryterad på mässan"), default);

        await _applicationRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Registration.Aggregates.StaffApplication>(a => a.InterestDescription == "Rekryterad på mässan"),
            Arg.Any<CancellationToken>());
    }
}
