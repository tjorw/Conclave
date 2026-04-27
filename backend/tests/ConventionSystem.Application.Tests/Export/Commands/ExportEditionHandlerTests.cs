using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Export.Abstractions;
using ConventionSystem.Application.Export.Commands.ExportEdition;
using ConventionSystem.Application.Export.Contracts;
using NSubstitute;

namespace ConventionSystem.Application.Tests.Export.Commands;

public class ExportEditionHandlerTests
{
    private readonly IEditionExportReadService _exportReadService = Substitute.For<IEditionExportReadService>();
    private readonly ExportEditionHandler _handler;

    public ExportEditionHandlerTests()
    {
        _handler = new ExportEditionHandler(_exportReadService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsDocumentAndJsonFileName()
    {
        var editionId = Guid.NewGuid();
        var document = CreateDocument("Konvent 2027");
        _exportReadService.BuildDocumentAsync(editionId, true, true, Arg.Any<CancellationToken>())
            .Returns(document);

        var result = await _handler.Handle(new ExportEditionCommand(editionId, true, true), default);

        Assert.Same(document, result.Document);
        Assert.Equal("konvent-2027-export.json", result.FileName);
    }

    [Fact]
    public async Task Handle_ForwardsIncludeFlags()
    {
        var editionId = Guid.NewGuid();
        _exportReadService.BuildDocumentAsync(editionId, false, true, Arg.Any<CancellationToken>())
            .Returns(CreateDocument("Konvent"));

        await _handler.Handle(new ExportEditionCommand(editionId, false, true), default);

        await _exportReadService.Received(1)
            .BuildDocumentAsync(editionId, false, true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditionNotFound_ThrowsResourceNotFoundException()
    {
        _exportReadService.BuildDocumentAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((EditionExportDocument?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _handler.Handle(new ExportEditionCommand(Guid.NewGuid(), false, false), default));
    }

    private static EditionExportDocument CreateDocument(string name)
        => new(
            EditionExportDocument.CurrentSchemaVersion,
            name,
            3,
            [],
            [],
            [],
            [],
            null,
            null);
}
