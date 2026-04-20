namespace ConventionSystem.Api.Services;

public interface IAuthLinkBuilder
{
    string BuildEmailConfirmationLink(string email, string token, Guid? tenantId = null);

    string BuildPasswordResetLink(string email, string token, Guid? tenantId = null);

    string BuildSignupConfirmationLink(string email, Guid tenantId, string subdomain, string token);

    string BuildTenantAdminLoginLink(string subdomain);
}
