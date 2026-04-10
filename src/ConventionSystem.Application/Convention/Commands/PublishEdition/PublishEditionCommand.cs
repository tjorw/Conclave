using MediatR;

namespace ConventionSystem.Application.Convention.Commands.PublishEdition;

public sealed record PublishEditionCommand(
    Guid EditionId) : IRequest;
