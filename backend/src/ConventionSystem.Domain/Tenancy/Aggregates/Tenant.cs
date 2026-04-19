using System.Text.RegularExpressions;
using ConventionSystem.Domain.Common;
using ConventionSystem.Domain.Tenancy.Enums;
using ConventionSystem.Domain.Tenancy.Events;
using ConventionSystem.Domain.Tenancy.Exceptions;
using ConventionSystem.Domain.Tenancy.Ids;

namespace ConventionSystem.Domain.Tenancy.Aggregates;

public sealed class Tenant : AggregateRoot
{
    private static readonly Regex SubdomainRegex = new("^[a-z0-9-]{3,63}$", RegexOptions.Compiled);

    public TenantId Id { get; private set; }
    public string Subdomain { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Tenant() { }

    public Tenant(TenantId id, string subdomain, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Visningsnamn får inte vara tomt.", nameof(displayName));

        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        if (!SubdomainRegex.IsMatch(normalizedSubdomain))
            throw new ArgumentException("Subdomän måste matcha formatet [a-z0-9-]{3,63}.", nameof(subdomain));

        Id = id;
        Subdomain = normalizedSubdomain;
        DisplayName = displayName.Trim();
        Status = TenantStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TenantCreated(Id, Subdomain, DisplayName, CreatedAt));
    }

    public void Suspend()
    {
        if (Status == TenantStatus.Suspended)
            throw new TenantAlreadySuspendedException();

        Status = TenantStatus.Suspended;
        RaiseDomainEvent(new TenantSuspended(Id, Subdomain, DateTimeOffset.UtcNow));
    }

    public void Restore()
    {
        if (Status == TenantStatus.Active)
            throw new TenantAlreadyActiveException();

        Status = TenantStatus.Active;
        RaiseDomainEvent(new TenantRestored(Id, Subdomain, DateTimeOffset.UtcNow));
    }
}