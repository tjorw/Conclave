using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Convention.Queries;
using ConventionSystem.Domain.Convention;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Queries.GetEditionContent;

public sealed class GetEditionContentHandler(IEditionRepository editionRepository)
    : IQueryHandler<GetEditionContentQuery, IReadOnlyList<EditionContentDto>>
{
    public async Task<IReadOnlyList<EditionContentDto>> Handle(GetEditionContentQuery query, CancellationToken ct)
    {
        var editionId = new EditionId(query.EditionId);
        var edition = await editionRepository.GetByIdWithContentAsync(editionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", query.EditionId.ToString());

        var storedValues = edition.Content.ToDictionary(c => c.Key, c => c.Value);

        return EditionContentKey.AllKeys
            .Select(key => new EditionContentDto(key, storedValues.GetValueOrDefault(key, string.Empty)))
            .ToList();
    }
}
