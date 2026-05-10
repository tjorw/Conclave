using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Commands.SetPageTranslation;

public sealed class SetPageTranslationHandler(IPageRepository pageRepository)
    : CommandHandler<SetPageTranslationCommand>
{
    protected override async Task ExecuteAsync(SetPageTranslationCommand command, CancellationToken ct)
    {
        var pageId = new PageId(command.PageId);
        var page = await pageRepository.GetByIdWithTranslationsAsync(pageId, ct)
            ?? throw new ResourceNotFoundException("Sida", command.PageId.ToString());

        page.SetTranslation(command.Locale, command.Title, command.Content);
        await pageRepository.SaveAsync(ct);
    }
}
