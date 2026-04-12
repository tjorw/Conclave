namespace ConventionSystem.Api.Auth;

public static class AuthConstants
{
    public static class Policies
    {
        public const string IsAdmin = "IsAdmin";
    }

    public static class Claims
    {
        public const string PersonId = "person_id";
        public const string IsAdmin = "is_admin";
    }
}
