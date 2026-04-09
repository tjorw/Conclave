using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CopyEditionStructure;

public sealed record CopyEditionStructureCommand(
    Guid TargetEditionId,
    Guid SourceEditionId,
    Guid PerformedById) : IRequest;
