using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Candidate.API.Infrastructure;

/// <summary>
/// Turns database failures that the client can act on into 409 problem responses. Services
/// check rules up front; this covers races between that check and the save (e.g. two users
/// editing the same provider). Details are logged, not returned to the client.
/// </summary>
public sealed class DatabaseExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<DatabaseExceptionHandler> logger) : IExceptionHandler
{
    private const int SqliteConstraintError = 19;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (title, detail) = exception switch
        {
            DbUpdateConcurrencyException =>
                ("Concurrency conflict", "The record was changed or deleted by someone else. Reload it and try again."),
            DbUpdateException { InnerException: SqliteException { SqliteErrorCode: SqliteConstraintError } } =>
                ("Data integrity conflict", "The change conflicts with existing data (for example a duplicate value)."),
            _ => (null, null)
        };

        if (title is null)
        {
            return false;
        }

        logger.LogWarning(exception, "{Title} on {Method} {Path}", title, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = title,
                Detail = detail
            }
        });
    }
}
