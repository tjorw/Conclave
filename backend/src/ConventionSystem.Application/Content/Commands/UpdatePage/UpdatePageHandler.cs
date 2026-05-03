using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Content.Commands.UpdatePage;

public sealed class UpdatePageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<UpdatePageCommand>
{
    protected override async Task ExecuteAsync(UpdatePageCommand command, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan uppdatera informationssidor.");

        var pageId = new PageId(command.PageId);
        var page = await pageRepository.GetByIdAsync(pageId, ct)
            ?? throw new KeyNotFoundException("Informationssidan hittades inte.");

        var editionId = command.EditionId.HasValue ? new EditionId(command.EditionId.Value) : (EditionId?)null;
        page.Update(command.Slug, command.Title, command.Content, editionId);

        if (await pageRepository.SlugExistsAsync(convention.Id, editionId, page.Slug, pageId, ct))
            throw new PageSlugAlreadyExistsException();

        await pageRepository.SaveAsync(ct);
    }
}
