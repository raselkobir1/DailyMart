using DailyMart.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DailyMart.API.ExceptionHandling;

/// <summary>
/// Catch-all for anything that reaches the pipeline unhandled (validation failures never get here -
/// see <see cref="Filters.ValidationFilter"/>, which short-circuits those before the action runs).
/// Maps known business exceptions to their proper status code, and anything else to a generic 500
/// instead of leaking a stack trace.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            FeatureNotEntitledException => (StatusCodes.Status403Forbidden, exception.Message),
            BusinessRuleException => (StatusCodes.Status400BadRequest, exception.Message),
            // A uniqueness check-then-insert (product code/barcode, category/brand/unit name, role name,
            // ...) has a TOCTOU race: two concurrent requests can both pass the app-level "does this
            // already exist" check before either commits, and only the DB's unique index catches the
            // second one - as a raw DbUpdateException that would otherwise fall through to the generic
            // 500 below instead of the same "already exists" 400 a non-racing duplicate gets.
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "This record conflicts with an existing one - it may have just been created by someone else. Please refresh and try again."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "{Method} {Path} rejected: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "Please contact support if the problem persists."
                : null,
            Instance = httpContext.Request.Path
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
