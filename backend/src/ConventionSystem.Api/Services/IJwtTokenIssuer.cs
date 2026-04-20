namespace ConventionSystem.Api.Services;

public interface IJwtTokenIssuer
{
    string Issue(
        Guid? personId,
        bool isAdmin,
        bool isSystemAdmin,
        string userType,
        Guid? tenantId);
}
