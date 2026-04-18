using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UnpublishEdition;

public sealed record UnpublishEditionCommand(
    Guid EditionId) : IRequest;
