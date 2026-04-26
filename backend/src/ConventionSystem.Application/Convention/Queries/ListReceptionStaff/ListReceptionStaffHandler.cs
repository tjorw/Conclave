using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.ListReceptionStaff;

public sealed class ListReceptionStaffHandler(
    IEditionRepository editionRepository,
    IPersonRepository personRepository)
    : IQueryHandler<ListReceptionStaffQuery, IReadOnlyList<ReceptionStaffMemberDto>>
{
    public async Task<IReadOnlyList<ReceptionStaffMemberDto>> Handle(ListReceptionStaffQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var edition = await editionRepository.GetByIdWithReceptionStaffAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", query.EditionId.ToString());

        if (edition.ReceptionStaff.Count == 0)
            return [];

        var persons = await personRepository.ListByConventionIdAsync(edition.ConventionId, ct);
        var personMap = persons.ToDictionary(p => p.Id);

        return edition.ReceptionStaff
            .Select(r => personMap.TryGetValue(r.PersonId.Value, out var p)
                ? new ReceptionStaffMemberDto(r.PersonId.Value, p.Name, p.Email, r.AddedAt, r.AddedById.Value)
                : new ReceptionStaffMemberDto(r.PersonId.Value, string.Empty, string.Empty, r.AddedAt, r.AddedById.Value))
            .ToList();
    }
}
