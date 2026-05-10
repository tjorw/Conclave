using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.GetEditionLocales;

public sealed class GetEditionLocalesHandler(IEditionRepository editionRepository)
    : IQueryHandler<GetEditionLocalesQuery, IReadOnlyList<EditionLocaleDto>>
{
    public async Task<IReadOnlyList<EditionLocaleDto>> Handle(GetEditionLocalesQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var edition = await editionRepository.GetByIdWithLocalesAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", query.EditionId.ToString());

        return edition.Locales
            .Select(l => new EditionLocaleDto(l.Locale, l.IsPrimary))
            .ToList();
    }
}
