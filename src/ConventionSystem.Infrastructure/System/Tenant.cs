namespace ConventionSystem.Infrastructure.System;

public sealed class Tenant
{
    private Tenant() { }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = default!;
    public string ConnectionString { get; private set; } = default!;
    public string? Domain { get; private set; }

    public static Tenant Create(Guid id, string slug, string connectionString, string? domain = null)
        => new()
        {
            Id = id,
            Slug = slug,
            ConnectionString = connectionString,
            Domain = domain
        };
}
