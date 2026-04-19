using MediatR;

namespace ConventionSystem.Application.Tenancy.Commands.CreateTenant;

public record CreateTenantCommand(string Subdomain, string DisplayName) : IRequest<Guid>;