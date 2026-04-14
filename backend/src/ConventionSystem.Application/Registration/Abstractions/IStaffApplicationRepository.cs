using ConventionSystem.Application.Staff.Queries;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Registration.Aggregates;
using ConventionSystem.Domain.Registration.Ids;

namespace ConventionSystem.Application.Registration.Abstractions;

public interface IStaffApplicationRepository
{
    Task<StaffApplication?> GetByIdAsync(StaffApplicationId id, CancellationToken ct = default);
    Task<StaffApplication?> GetByIdWithDetailsAsync(StaffApplicationId id, CancellationToken ct = default);
    Task<bool> HasActiveApplicationAsync(PersonId personId, EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<StaffApplicationSummaryDto>> ListByEditionIdAsync(EditionId editionId, CancellationToken ct = default);
    Task<IReadOnlyList<EditionStaffMemberDto>> ListApprovedByEditionIdAsync(EditionId editionId, CancellationToken ct = default);
    Task AddAndSaveAsync(StaffApplication application, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
