using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;
using ConventionAggregate = ConventionSystem.Domain.Convention.Aggregates.Convention;
using EditionAggregate = ConventionSystem.Domain.Convention.Aggregates.Edition;

namespace ConventionSystem.Application.Common.Contexts;

public static class StaffApplicationContextLoader
{
    public static async Task<StaffApplicationContext> LoadAsync(
        IStaffApplicationRepository staffApplicationRepository,
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        StaffApplicationId applicationId,
        CancellationToken ct)
    {
        var application = await staffApplicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansokan", applicationId.Value.ToString());

        return await CreateContextAsync(application, editionRepository, conventionRepository, ct);
    }

    public static async Task<StaffApplicationContext> LoadWithDetailsAsync(
        IStaffApplicationRepository staffApplicationRepository,
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        StaffApplicationId applicationId,
        CancellationToken ct)
    {
        var application = await staffApplicationRepository.GetByIdWithDetailsAsync(applicationId, ct)
            ?? throw new ResourceNotFoundException("Staffansokan", applicationId.Value.ToString());

        return await CreateContextAsync(application, editionRepository, conventionRepository, ct);
    }

    private static async Task<StaffApplicationContext> CreateContextAsync(
        StaffApplication application,
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByIdAsync(application.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", application.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        return new StaffApplicationContext(application, edition, convention);
    }
}

public sealed record StaffApplicationContext(
    StaffApplication Application,
    EditionAggregate Edition,
    ConventionAggregate Convention);
