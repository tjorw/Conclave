using ConventionSystem.Application.Common;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Event.Ids;
using MediatR;

namespace ConventionSystem.Application.Event.Commands.ChangeCategory;

public sealed class ChangeCategoryHandler(
    IEventRepository eventRepository,
    IEditionRepository editionRepository,
    IConventionRepository conventionRepository,
    ICurrentUser currentUser) : IRequestHandler<ChangeCategoryCommand>
{
    public async Task Handle(ChangeCategoryCommand command, CancellationToken ct)
    {
        var ev = await eventRepository.GetByIdAsync(new EventId(command.EventId), ct)
            ?? throw new InvalidOperationException("Evenemanget hittades inte.");

        var edition = await editionRepository.GetByIdWithCategoriesAsync(ev.EditionId, ct)
            ?? throw new InvalidOperationException("Upplagan hittades inte.");

        var convention = await conventionRepository.GetByIdAsync(edition.ConventionId, ct)
            ?? throw new InvalidOperationException("Konventionen hittades inte.");

        if (!convention.IsAdministrator(currentUser.PersonId))
            throw new UnauthorizedAccessException("Utföraren är inte administratör.");

        var newCategoryId = new CategoryId(command.CategoryId);
        if (!edition.Categories.Any(c => c.Id == newCategoryId))
            throw new InvalidOperationException("Kategorin finns inte i upplagan.");

        ev.ChangeCategory(newCategoryId);
        await eventRepository.SaveAsync(ct);
    }
}
