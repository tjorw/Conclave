namespace ConventionSystem.Api.Auth;

public static class AuthConstants
{
    public static class Frontend
    {
        public const string DefaultUrl = "http://localhost:4201";
    }

    public static class Policies
    {
        public const string IsAdmin = "IsAdmin";
        public const string IsSystemAdmin = "IsSystemAdmin";
    }

    public static class Claims
    {
        public const string PersonId = "person_id";
        public const string IsAdmin = "is_admin";
        public const string IsAdminTrue = "true";
        public const string IsSystemAdmin = "is_system_admin";
        public const string IsSystemAdminTrue = "true";
    }
}
