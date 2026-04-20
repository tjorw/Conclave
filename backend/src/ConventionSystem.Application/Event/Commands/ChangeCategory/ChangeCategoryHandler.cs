using ConventionSystem.Application.Common;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;

namespace ConventionSystem.Application.Event.Commands.ChangeCategory;

public sealed class ChangeCategoryHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : CommandHandler<ChangeCategoryCommand>
{
    protected override async Task ExecuteAsync(ChangeCategoryCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new ResourceNotFoundException("Evenemang", command.EventId.ToString());

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new ResourceNotFoundException("Upplaga", ev.EditionId.Value.ToString());

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", edition.ConventionId.Value.ToString());

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new UnauthorizedAccessException("Utföraren är inte administratör.");

        var newCategoryId = new CategoryId(command.CategoryId);
        if (!edition.Categories.Any(c => c.Id == newCategoryId))
            throw new InvalidOperationException("Kategorin finns inte i upplagan.");

        ev.ChangeCategory(newCategoryId);
        await eventRepository.SaveAsync(ct);
    }
}
