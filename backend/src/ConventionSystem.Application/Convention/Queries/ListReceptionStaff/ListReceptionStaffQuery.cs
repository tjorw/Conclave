
namespace ConventionSystem.Application.Convention.Queries.ListReceptionStaff;

public sealed record ListReceptionStaffQuery(Guid EditionId) : IQuery<IReadOnlyList<ReceptionStaffMemberDto>>;

public sealed record ReceptionStaffMemberDto(
    Guid PersonId,
    string Name,
    string Email,
    DateTimeOffset AddedAt,
    Guid AddedById);
