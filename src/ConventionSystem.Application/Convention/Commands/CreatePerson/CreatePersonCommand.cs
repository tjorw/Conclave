using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreatePerson;

public sealed record CreatePersonCommand(
    Guid ConventionId,
    string Name,
    string Email,
    string? Phone) : IRequest<Guid>;
