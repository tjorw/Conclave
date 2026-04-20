using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionAggregate = ConventionSystem.Domain.Convention.Aggregates.Convention;
using EditionAggregate = ConventionSystem.Domain.Convention.Aggregates.Edition;

namespace ConventionSystem.Application.Common.Contexts;

public static class ShiftContextLoader
{
    public static async Task<ShiftContext> LoadAsync(
        IShiftRepository shiftRepository,
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        ShiftId shiftId,
        CancellationToken ct)
    {
        var shift = await shiftRepository.GetByIdAsync(shiftId, ct)
            ?? throw new ResourceNotFoundException("Pass", shiftId.Value.ToString());

        return await CreateContextAsync(shift, editionRepository, conventionRepository, ct);
    }

    public static async Task<ShiftContext> LoadWithAssignmentsAsync(
        IShiftRepository shiftRepository,
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        ShiftId shiftId,
        CancellationToken ct)
    {
        var shift = await shiftRepository.GetByIdWithAssignmentsAsync(shiftId, ct)
            ?? throw new ResourceNotFoundException("Pass", shiftId.Value.ToString());

        return await CreateContextAsync(shift, editionRepository, conventionRepository, ct);
    }

    private static async Task<ShiftContext> CreateContextAsync(
        Shift shift,
        IEditionRepository editionRepository,
        IConventionRepository conventionRepository,
        CancellationToken ct)
    {
        var edition = await editionRepository.GetByStationIdAsync(shift.StationId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", shift.StationId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        return new ShiftContext(shift, edition, convention);
    }
}

public sealed record ShiftContext(
    Shift Shift,
    EditionAggregate Edition,
    ConventionAggregate Convention);
