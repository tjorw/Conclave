using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Commands.UnpublishPage;

public sealed class UnpublishPageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<UnpublishPageCommand>
{
    protected override async Task ExecuteAsync(UnpublishPageCommand command, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan avpublicera informationssidor.");

        var page = await pageRepository.GetByIdAsync(new PageId(command.PageId), ct)
            ?? throw new KeyNotFoundException("Informationssidan hittades inte.");

        page.Unpublish();
        await pageRepository.SaveAsync(ct);
    }
}
