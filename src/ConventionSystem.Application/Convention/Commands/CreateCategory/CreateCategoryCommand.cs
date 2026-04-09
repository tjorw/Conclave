using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    Guid EditionId,
    string Name,
    string? Description,
    Guid ResponsibleId,
    Guid PerformedById) : IRequest<Guid>;
