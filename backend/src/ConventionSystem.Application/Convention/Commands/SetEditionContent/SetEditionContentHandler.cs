using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Contexts;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Domain.Convention;
using ConventionSystem.Domain.Convention.Ids;

namespace ConventionSystem.Application.Convention.Commands.SetEditionContent;

public sealed class SetEditionContentHandler(
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser)
    : CommandHandler<SetEditionContentCommand>
{
    protected override async Task ExecuteAsync(SetEditionContentCommand command, CancellationToken ct)
    {
        var editionId = new EditionId(command.EditionId);
        var context = await EditionContextLoader.LoadWithContentAsync(editionRepository, conventionRepository, editionId, ct);

        if (!context.Convention.IsAdministrator(currentUser.PersonId))
            throw new ForbiddenException("Utföraren är inte administratör för denna konvention.");

        foreach (var item in command.Items)
        {
            if (!EditionContentKey.AllKeys.Contains(item.Key))
                throw new InvalidOperationException($"Okänd innehållsnyckel: '{item.Key}'.");

            context.Edition.SetContent(item.Key, item.Value);
        }

        await editionRepository.SaveAsync(ct);
    }
}
