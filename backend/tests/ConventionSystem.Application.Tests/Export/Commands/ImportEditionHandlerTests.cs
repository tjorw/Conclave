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

    [Fact]
    public async Task Handle_CategoryDescriptions_MapsOrganizerAndPublicDescription()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);

        var document = MinimalDocument() with
        {
            Categories =
            [
                new ExportCategoryDto(
                    "Rollspel",
                    "Arrangorsinstruktion",
                    "Publik beskrivning",
                    null,
                    null),
            ],
        };

        await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), document),
            default);

        await _editionRepo.Received(1).AddAndSaveAsync(
            Arg.Is<Domain.Convention.Aggregates.Edition>(edition =>
                edition.Categories.Count == 1 &&
                edition.Categories[0].OrganizerInstructions == "Arrangorsinstruktion" &&
                edition.Categories[0].PublicDescription == "Publik beskrivning"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LegacyCategoryDescription_FallsBackToPublicDescription()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);
        Domain.Convention.Aggregates.Edition? capturedEdition = null;
        _editionRepo
            .When(r => r.AddAndSaveAsync(Arg.Any<Domain.Convention.Aggregates.Edition>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedEdition = call.Arg<Domain.Convention.Aggregates.Edition>());

        var document = MinimalDocument() with
        {
            SchemaVersion = 1,
            Categories =
            [
                new ExportCategoryDto(
                    "Bradspel",
                    null,
                    null,
                    "Legacy publikt innehall",
                    null),
            ],
        };

        await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), document),
            default);

        Assert.NotNull(capturedEdition);
        Assert.Single(capturedEdition.Categories);
        Assert.Null(capturedEdition.Categories[0].OrganizerInstructions);
        Assert.Equal("Legacy publikt innehall", capturedEdition.Categories[0].PublicDescription);
    }

    [Fact]
    public async Task Handle_EventCoOrganiserLimit_SetsImportedValue()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);
        Domain.Event.Aggregates.Event? capturedEvent = null;
        _eventRepo
            .When(r => r.AddAndSaveAsync(Arg.Any<Domain.Event.Aggregates.Event>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedEvent = call.Arg<Domain.Event.Aggregates.Event>());

        var document = MinimalDocument() with
        {
            Categories =
            [
                new ExportCategoryDto(
                    "Seminarier",
                    null,
                    null,
                    null,
                    null),
            ],
            Events =
            [
                new ExportEventDto(
                    "Event 1",
                    "Beskrivning",
                    "Seminarier",
                    "DropIn",
                    null,
                    null,
                    3,
                    null,
                    []),
            ],
        };

        await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), document),
            default);

        Assert.NotNull(capturedEvent);
        Assert.Equal(3, capturedEvent.CoOrganiserLimit);
    }

    [Fact]
    public async Task Handle_LegacyEventCoOrganiserCount_FallsBackToLimit()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);
        Domain.Event.Aggregates.Event? capturedEvent = null;
        _eventRepo
            .When(r => r.AddAndSaveAsync(Arg.Any<Domain.Event.Aggregates.Event>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedEvent = call.Arg<Domain.Event.Aggregates.Event>());

        var document = MinimalDocument() with
        {
            SchemaVersion = 1,
            Categories =
            [
                new ExportCategoryDto(
                    "Seminarier",
                    null,
                    null,
                    null,
                    null),
            ],
            Events =
            [
                new ExportEventDto(
                    "Event Legacy",
                    "Beskrivning",
                    "Seminarier",
                    "DropIn",
                    null,
                    null,
                    0,
                    null,
                    [],
                    4),
            ],
        };

        await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), document),
            default);

        Assert.NotNull(capturedEvent);
        Assert.Equal(4, capturedEvent.CoOrganiserLimit);
    }

    [Fact]
    public async Task Handle_ProgramTagDefinitions_AreImportedOnEdition()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);
        Domain.Convention.Aggregates.Edition? capturedEdition = null;
        _editionRepo
            .When(r => r.AddAndSaveAsync(Arg.Any<Domain.Convention.Aggregates.Edition>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedEdition = call.Arg<Domain.Convention.Aggregates.Edition>());

        var document = MinimalDocument() with
        {
            ProgramTagDefinitions = ["Barnvanligt", "Nyborgare"],
        };

        await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), document),
            default);

        Assert.NotNull(capturedEdition);
        Assert.Equal(2, capturedEdition.ProgramTagDefinitions.Count);
        Assert.Equal("Barnvanligt", capturedEdition.ProgramTagDefinitions[0].Name);
        Assert.Equal("Nyborgare", capturedEdition.ProgramTagDefinitions[1].Name);
    }

    [Fact]
    public async Task Handle_EventProgramTags_ImportsKnownTagsAndWarnsForUnknown()
    {
        var (convention, admin) = SetupAdminConvention();
        _currentUser.PersonId.Returns(admin.Id);
        Domain.Event.Aggregates.Event? capturedEvent = null;
        _eventRepo
            .When(r => r.AddAndSaveAsync(Arg.Any<Domain.Event.Aggregates.Event>(), Arg.Any<CancellationToken>()))
            .Do(call => capturedEvent = call.Arg<Domain.Event.Aggregates.Event>());

        var document = MinimalDocument() with
        {
            ProgramTagDefinitions = ["Barnvanligt"],
            Categories =
            [
                new ExportCategoryDto(
                    "Seminarier",
                    null,
                    null,
                    null,
                    null),
            ],
            Events =
            [
                new ExportEventDto(
                    "Event 1",
                    "Beskrivning",
                    "Seminarier",
                    "DropIn",
                    null,
                    null,
                    1,
                    null,
                    [],
                    null,
                    ["Barnvanligt", "Saknas"]),
            ],
        };

        var result = await _handler.Handle(
            new ImportEditionCommand(convention.Id.Value, "Importerad", new DateOnly(2028, 3, 1), document),
            default);

        Assert.NotNull(capturedEvent);
        Assert.Single(capturedEvent.ProgramTags);
        Assert.Equal("Barnvanligt", capturedEvent.ProgramTags[0].Name);
        Assert.Contains(result.Warnings, warning =>
            warning.Code == "ProgramTagSkipped" && warning.Message.Contains("Saknas", StringComparison.Ordinal));
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
