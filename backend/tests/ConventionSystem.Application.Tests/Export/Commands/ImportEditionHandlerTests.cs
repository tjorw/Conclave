using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Export.Commands.ImportEdition;
using ConventionSystem.Application.Export.Contracts;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Export.Commands;

public class ImportEditionHandlerTests
{
    private readonly IConventionRepository _conventionRepo = Substitute.For<IConventionRepository>();
    private readonly IPersonRepository _personRepo = Substitute.For<IPersonRepository>();
    private readonly IEditionRepository _editionRepo = Substitute.For<IEditionRepository>();
    private readonly ITicketTypeRepository _ticketTypeRepo = Substitute.For<ITicketTypeRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IShiftRepository _shiftRepo = Substitute.For<IShiftRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private readonly ImportEditionHandler _handler;

    public ImportEditionHandlerTests()
    {
        _handler = new ImportEditionHandler(
            _conventionRepo,
            _personRepo,
            _editionRepo,
            _ticketTypeRepo,
            _eventRepo,
            _shiftRepo,
            _currentUser);
    }

    [Fact]
    public async Task Handle_MinimalDocument_CreatesEdition()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);

        var result = await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), MinimalDocument()),
            default);

        Assert.NotEqual(Guid.Empty, result.EditionId);
        Assert.Empty(result.Warnings);
        await _editionRepo.Received(1).AddAndSaveAsync(
            Arg.Any<Domain.Convention.Aggregates.Edition>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownSchemaVersion_ThrowsArgumentException()
    {
        var document = MinimalDocument() with { SchemaVersion = 999 };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(new ImportEditionCommand(Guid.NewGuid(), "Importerad", new DateOnly(2028, 3, 1), document), default));
    }

    [Fact]
    public async Task Handle_PerformerNotAdministrator_ThrowsForbiddenException()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        var nonAdmin = convention.CreatePerson("Non Admin", "nonadmin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(nonAdmin.Id, Arg.Any<CancellationToken>()).Returns(nonAdmin);
        _currentUser.PersonId.Returns(nonAdmin.Id);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(
                new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), MinimalDocument()),
                default));
    }

    private (Domain.Convention.Aggregates.Convention Convention, Domain.Convention.Entities.Person Admin) SetupAdminConvention()
    {
        var convention = new Domain.Convention.Aggregates.Convention(ConventionId.New(), "Test Con", "test-con");
        var admin = convention.RegisterPerson("Admin", "admin@example.com");
        convention.AddAdministrator(admin.Id, admin.Id);

        _conventionRepo.GetByIdAsync(convention.Id, Arg.Any<CancellationToken>()).Returns(convention);
        _personRepo.GetByIdAsync(admin.Id, Arg.Any<CancellationToken>()).Returns(admin);

        return (convention, admin);
    }

    private static EditionExportDocument MinimalDocument()
        => new(
            EditionExportDocument.CurrentSchemaVersion,
            "Källa",
            3,
            [],
            [],
            [],
            [],
            null,
            null);
}
