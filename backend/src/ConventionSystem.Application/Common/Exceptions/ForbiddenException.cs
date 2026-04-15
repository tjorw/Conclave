namespace ConventionSystem.Application.Common.Exceptions;

public sealed class ForbiddenException(string message)
    : Exception(message)
{
    public string ErrorCode => "forbidden";
}
