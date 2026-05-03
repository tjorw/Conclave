using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Commands.DeletePage;

public sealed class DeletePageHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<DeletePageCommand>
{
    protected override async Task ExecuteAsync(DeletePageCommand command, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan ta bort informationssidor.");

        var page = await pageRepository.GetByIdAsync(new PageId(command.PageId), ct)
            ?? throw new KeyNotFoundException("Informationssidan hittades inte.");

        pageRepository.Remove(page);
        await pageRepository.SaveAsync(ct);
    }
}
