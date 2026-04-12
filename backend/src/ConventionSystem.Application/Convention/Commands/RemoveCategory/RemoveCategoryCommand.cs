using MediatR;

namespace ConventionSystem.Application.Convention.Commands.RemoveCategory;

public sealed record RemoveCategoryCommand(Guid EditionId, Guid CategoryId) : IRequest;
