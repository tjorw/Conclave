using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Commands.PublishPage;

public sealed class PublishPageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<PublishPageCommand>
{
    protected override async Task ExecuteAsync(PublishPageCommand command, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan publicera informationssidor.");

        var page = await pageRepository.GetByIdAsync(new PageId(command.PageId), ct)
            ?? throw new KeyNotFoundException("Informationssidan hittades inte.");

        page.Publish();
        await pageRepository.SaveAsync(ct);
    }
}
