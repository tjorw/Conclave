namespace ConventionSystem.Application.Common.Exceptions;

public sealed class PageSlugAlreadyExistsException()
    : Exception("Sluggen finns redan i valt scope.")
{
    public string ErrorCode => "page_slug_already_exists";
}
