using System.Net.Sockets;
using Npgsql;

namespace TransactionTypeService.Api.Errors;

/// <summary>Maps exceptions to RFC 7807 responses with a machine code (legacy 9999-FORMAT-DB2-MESSAGE, without leaking SQL text).</summary>
public class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, code, message, errors) = Map(ex, context.TraceIdentifier);
            if (status >= 500)
            {
                logger.LogError(ex, "Request failed with {Code} (trace {TraceId})", code, context.TraceIdentifier);
            }
            else if (ex is not ServiceException)
            {
                logger.LogWarning(ex, "Request rejected with {Code} (trace {TraceId})", code, context.TraceIdentifier);
            }
            await WriteAsync(context, status, code, message, errors);
        }
    }

    public static Task WriteAsync(HttpContext context, int status, string code, string message,
        IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = message,
            status,
            code,
            message,
            errors,
            traceId = context.TraceIdentifier
        }, options: null, contentType: "application/problem+json");
    }

    private static (int, string, string, IDictionary<string, string[]>?) Map(Exception ex, string traceId)
    {
        if (ex is ServiceException service)
        {
            var status = service.Kind switch
            {
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.NoChanges => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status400BadRequest
            };
            return (status, service.Code, service.Message, service.Errors);
        }

        var postgres = Find<PostgresException>(ex);
        if (postgres is not null)
        {
            return postgres.SqlState switch
            {
                PostgresErrorCodes.ForeignKeyViolation => (409, ErrorCodes.HasDependentRecords,
                    "Please delete associated child records first.", null),
                PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.LockNotAvailable
                    or PostgresErrorCodes.SerializationFailure => (409, ErrorCodes.RecordLocked,
                    "The record is being changed by someone else. Try again.", null),
                _ when postgres.SqlState.StartsWith("08") || postgres.SqlState.StartsWith("57P") =>
                    (503, ErrorCodes.DbUnavailable, "The service is temporarily unavailable. Try again later.", null),
                _ => (500, ErrorCodes.InternalError, $"Something went wrong. Reference: {traceId}.", null)
            };
        }

        if (Find<NpgsqlException>(ex) is not null || Find<SocketException>(ex) is not null || Find<TimeoutException>(ex) is not null)
        {
            return (503, ErrorCodes.DbUnavailable, "The service is temporarily unavailable. Try again later.", null);
        }

        return (500, ErrorCodes.InternalError, $"Something went wrong. Reference: {traceId}.", null);
    }

    private static T? Find<T>(Exception? ex) where T : Exception
    {
        while (ex is not null)
        {
            if (ex is T match)
            {
                return match;
            }
            ex = ex.InnerException;
        }
        return null;
    }
}
