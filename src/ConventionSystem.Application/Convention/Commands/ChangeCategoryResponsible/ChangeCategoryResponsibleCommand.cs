using MediatR;

namespace ConventionSystem.Application.Convention.Commands.ChangeCategoryResponsible;

public sealed record ChangeCategoryResponsibleCommand(
    Guid EditionId,
    Guid CategoryId,
    Guid NewResponsibleId) : IRequest;
