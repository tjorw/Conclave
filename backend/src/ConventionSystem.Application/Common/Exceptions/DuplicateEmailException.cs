namespace ConventionSystem.Application.Common.Exceptions;

public sealed class DuplicateEmailException(string email)
    : Exception($"E-postadressen '{email}' är redan registrerad i denna konvention.")
{
    public string ErrorCode => "duplicate_email";
}
