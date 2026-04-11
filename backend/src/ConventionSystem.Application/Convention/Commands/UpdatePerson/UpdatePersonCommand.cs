using MediatR;

namespace ConventionSystem.Application.Convention.Commands.UpdatePerson;

public sealed record UpdatePersonCommand(
    Guid PersonId,
    string Name,
    string Email,
    string? Phone) : IRequest;
