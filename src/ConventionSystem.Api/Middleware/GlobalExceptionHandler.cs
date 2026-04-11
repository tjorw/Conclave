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
        var (statusCode, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Ogiltiga parametrar"),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity, "Affärsregelbrott"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Ej behörig"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resursen hittades inte"),
            _ => (StatusCodes.Status500InternalServerError, "Ett internt serverfel inträffade")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Ohanterat undantag");

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
