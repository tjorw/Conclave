using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Content.Commands.CreatePage;

public sealed class CreatePageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : ICommandHandler<CreatePageCommand, Guid>
{
    public async Task<Guid> Handle(CreatePageCommand command, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan skapa informationssidor.");

        var editionId = command.EditionId.HasValue ? new EditionId(command.EditionId.Value) : (EditionId?)null;
        var page = new Page(PageId.New(), convention.Id, editionId, command.Slug, command.Title, command.Content);

        if (await pageRepository.SlugExistsAsync(convention.Id, editionId, page.Slug, null, ct))
            throw new PageSlugAlreadyExistsException();

        await pageRepository.AddAsync(page, ct);
        await pageRepository.SaveAsync(ct);

        return page.Id.Value;
    }
}
