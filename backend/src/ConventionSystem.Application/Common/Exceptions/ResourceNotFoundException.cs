namespace ConventionSystem.Application.Common.Exceptions;

public sealed class ResourceNotFoundException(string resourceName, string resourceId)
    : Exception($"{resourceName} '{resourceId}' hittades inte.")
{
    public string ErrorCode => "resource_not_found";
}
