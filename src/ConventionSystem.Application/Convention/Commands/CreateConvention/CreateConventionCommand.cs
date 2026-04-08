using MediatR;

namespace ConventionSystem.Application.Convention.Commands.CreateConvention;

public record CreateConventionCommand(
    string Name,
    string Slug,
    string RegistrantName,
    string RegistrantEmail) : IRequest<Guid>;
