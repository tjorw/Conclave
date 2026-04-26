using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionAggregate = ConventionSystem.Domain.Convention.Aggregates.Convention;
using EditionAggregate = ConventionSystem.Domain.Convention.Aggregates.Edition;
using EditionId = ConventionSystem.Domain.Convention.Ids.EditionId;

namespace ConventionSystem.Application.Common.Contexts;

public static class EditionContextLoader
{
    public static async Task<EditionContext> LoadWithStructureAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdWithStructureAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{editionId.Value}' hittades inte.");

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    public static async Task<EditionContext> LoadAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{editionId.Value}' hittades inte.");

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    public static async Task<EditionContext> LoadWithStaffAreasAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdWithStaffAreasAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{editionId.Value}' hittades inte.");

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    public static async Task<EditionContext> LoadWithCategoriesAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdWithCategoriesAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", editionId.Value.ToString());

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    public static async Task<EditionContext> LoadWithCategoriesForConventionCommandAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdWithCategoriesAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{editionId.Value}' hittades inte.");

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    public static async Task<EditionContext> LoadWithReceptionStaffAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdWithReceptionStaffAsync(editionId, ct)
            ?? throw new InvalidOperationException($"Upplaga '{editionId.Value}' hittades inte.");

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    public static async Task<EditionContext> LoadWithCategoriesAndVenuesAsync(
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        EditionId editionId,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdWithCategoriesAndVenuesAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", editionId.Value.ToString());

        return await CreateContextAsync(conventionRepository, edition, ct);
    }

    private static async Task<EditionContext> CreateContextAsync(
        IConventionRepository conventionRepository,
        EditionAggregate edition,
        CancellationToken ct)
    {
        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        return new EditionContext(edition, convention);
    }
}

public sealed record EditionContext(
    EditionAggregate Edition,
    ConventionAggregate Convention);
