using ConventionSystem.Api.Auth;

namespace ConventionSystem.Api.Services;

public sealed class AuthLinkBuilder(IConfiguration configuration) : IAuthLinkBuilder
{
    public string BuildEmailConfirmationLink(string email, string token, Guid? tenantId = null)
    {
        var link = $"{ResolveFrontendUrl()}/confirm-email" +
                   $"?email={Uri.EscapeDataString(email)}" +
                   $"&token={Uri.EscapeDataString(token)}";

        if (tenantId.HasValue)
            link += $"&tenantId={tenantId.Value}";

        return link;
    }

    public string BuildPasswordResetLink(string email, string token, Guid? tenantId = null)
    {
        var link = $"{ResolveFrontendUrl()}/reset-password" +
                   $"?email={Uri.EscapeDataString(email)}" +
                   $"&token={Uri.EscapeDataString(token)}";

        if (tenantId.HasValue)
            link += $"&tenantId={tenantId.Value}";

        return link;
    }

    public string BuildSignupConfirmationLink(string email, Guid tenantId, string subdomain, string token)
    {
        var portalUrl = configuration["App:PortalUrl"] ?? "http://localhost:4202";

        return $"{portalUrl}/signup/confirm-email" +
               $"?email={Uri.EscapeDataString(email)}" +
               $"&token={Uri.EscapeDataString(token)}" +
               $"&tenantId={tenantId}" +
               $"&subdomain={Uri.EscapeDataString(subdomain)}";
    }

    public string BuildTenantAdminLoginLink(string subdomain)
    {
        var template = configuration["App:AdminUrlTemplate"] ?? "http://localhost:4200";
        var baseUrl = template.Replace("{subdomain}", Uri.EscapeDataString(subdomain), StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        return $"{baseUrl}/login";
    }

    private string ResolveFrontendUrl()
        => configuration["App:FrontendUrl"] ?? AuthConstants.Frontend.DefaultUrl;
}
