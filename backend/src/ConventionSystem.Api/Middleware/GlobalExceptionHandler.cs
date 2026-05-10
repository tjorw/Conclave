using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ConventionSystem.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errorCode) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Ogiltiga parametrar", "invalid_argument"),
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Resursen hittades inte", "resource_not_found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Saknar behörighet", "forbidden"),
            DuplicateEmailException dup => (StatusCodes.Status409Conflict, "E-postadressen används redan", dup.ErrorCode),
            PageSlugAlreadyExistsException pageSlugAlreadyExists => (StatusCodes.Status422UnprocessableEntity, "Affärsregelbrott", pageSlugAlreadyExists.ErrorCode),
            DomainRuleViolationException domainRule => (StatusCodes.Status422UnprocessableEntity, "Affärsregelbrott", domainRule.ErrorCode),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "Affärsregelbrott", "invalid_operation"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Ej behörig", "unauthorized"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resursen hittades inte", "key_not_found"),
            _ => (StatusCodes.Status500InternalServerError, "Ett internt serverfel inträffade", "internal_server_error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Ohanterat undantag");

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };
        problem.Extensions["errorCode"] = errorCode;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
