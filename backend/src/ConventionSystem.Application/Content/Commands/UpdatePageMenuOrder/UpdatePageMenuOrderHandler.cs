using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Authorization;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Content.Ids;

namespace ConventionSystem.Application.Content.Commands.UpdatePageMenuOrder;

public sealed class UpdatePageMenuOrderHandler(
    IPageRepository pageRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<UpdatePageMenuOrderCommand>
{
    protected override async Task ExecuteAsync(UpdatePageMenuOrderCommand command, CancellationToken ct)
    {
        var convention = await conventionRepository.GetSingleAsync(ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        ApplicationAuthorization.EnsureConventionAdmin(
            convention,
            currentUser.PersonId,
            "Endast administratörer kan uppdatera menyordning för informationssidor.");

        var page = await pageRepository.GetByIdAsync(new PageId(command.PageId), ct)
            ?? throw new KeyNotFoundException("Informationssidan hittades inte.");

        page.SetMenuSortOrder(command.MenuSortOrder);

        await pageRepository.SaveAsync(ct);
    }
}
