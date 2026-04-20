namespace ConventionSystem.Application.Tenancy.Abstractions;

public interface ISystemTenantReadService
{
    Task<IReadOnlyList<SystemTenantConventionDto>> ListConventionsAsync(Guid tenantId, CancellationToken ct = default);
}

public record SystemTenantConventionDto(Guid Id, string Name, string Slug);
