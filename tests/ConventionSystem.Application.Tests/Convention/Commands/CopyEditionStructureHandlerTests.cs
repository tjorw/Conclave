using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Commands.CopyEditionStructure;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Convention.Commands;

public class CopyEditionStructureHandlerTests
{
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly CopyEditionStructureHandler _handler;

    public CopyEditionStructureHandlerTests()
    {
        _handler = new CopyEditionStructureHandler(_editionRepo, _conventionRepo);
    }

    private (Domain.Convention.Aggregates.Convention convention,
             Domain.Convention.Entities.Person admin,
             Domain.Convention.Aggregates.Edition source,
             Domain.Convention.Aggregates.Edition target) Setup()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        var staff = convention.CreatePerson("Staff", "staff@example.com");
        var evt = convention.CreatePerson("Event", "event@example.com");
        var areaResponsible = convention.CreatePerson("Områdesansvarig", "area@example.com");

        var period1 = new DatePeriod(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3));
        var source = convention.CreateEdition("Konvent 2026", period1, staff.Id, evt.Id);
        source.CreateVenue("Stora salen", "Huvudbyggnad");
        var staffArea = source.CreateStaffArea("Reception", areaResponsible.Id);
        source.CreateStation("Reception A", staffArea.Id);

        var period2 = new DatePeriod(new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 3));
        var target = convention.CreateEdition("Konvent 2027", period2, staff.Id, evt.Id);

        _editionRepo.GetByIdWithStructureAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        _editionRepo.GetByIdWithStructureAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);

        return (convention, admin, source, target);
    }

    [Fact]
    public async Task Handle_ValidCommand_CopiesStructure()
    {
        var (_, admin, source, target) = Setup();

        await _handler.Handle(new CopyEditionStructureCommand(target.Id.Value, source.Id.Value, admin.Id.Value), default);

        Assert.Single(target.Venues);
        Assert.Single(target.StaffAreas);
        Assert.Single(target.Stations);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSave()
    {
        var (_, admin, source, target) = Setup();

        await _handler.Handle(new CopyEditionStructureCommand(target.Id.Value, source.Id.Value, admin.Id.Value), default);

        await _editionRepo.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TargetNotFound_Throws()
    {
        _editionRepo.GetByIdWithStructureAsync(Arg.Any<EditionId>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Convention.Aggregates.Edition?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CopyEditionStructureCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_Throws()
    {
        var (convention, _, source, target) = Setup();
        var nonAdmin = convention.CreatePerson("NonAdmin", "nonadmin@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CopyEditionStructureCommand(target.Id.Value, source.Id.Value, nonAdmin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_SourceFromDifferentConvention_Throws()
    {
        var (_, admin, _, target) = Setup();

        var otherConvention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Other Con", "other-con");
        var otherStaff = otherConvention.CreatePerson("Staff2", "staff2@example.com");
        var otherEvt = otherConvention.CreatePerson("Event2", "event2@example.com");
        var otherPeriod = new DatePeriod(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3));
        var otherSource = otherConvention.CreateEdition("Other 2026", otherPeriod, otherStaff.Id, otherEvt.Id);

        _editionRepo.GetByIdWithStructureAsync(otherSource.Id, Arg.Any<CancellationToken>()).Returns(otherSource);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CopyEditionStructureCommand(target.Id.Value, otherSource.Id.Value, admin.Id.Value), default));
    }

    [Fact]
    public async Task Handle_TargetIsPublished_Throws()
    {
        var (_, admin, source, target) = Setup();
        target.Publish(admin.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(new CopyEditionStructureCommand(target.Id.Value, source.Id.Value, admin.Id.Value), default));
    }
}
