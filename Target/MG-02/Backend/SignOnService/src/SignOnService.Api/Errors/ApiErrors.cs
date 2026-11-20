namespace SignOnService.Api.Errors;

public static class ErrorCodes
{
    public const string UserIdRequired = "USER_ID_REQUIRED";
    public const string PasswordRequired = "PASSWORD_REQUIRED";
    public const string FieldTooLong = "FIELD_TOO_LONG";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string WrongPassword = "WRONG_PASSWORD";
    public const string UnableToVerify = "UNABLE_TO_VERIFY";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string Internal = "INTERNAL_ERROR";
}

public sealed class ApiException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

/// <summary>Maps exceptions to RFC 7807 bodies with a machine <c>code</c>.</summary>
public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await WriteAsync(context, ex.Status, ex.Code, ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteAsync(context, ex.StatusCode == 413 ? 413 : 400, ErrorCodes.ValidationFailed, "Request body is invalid.");
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Unhandled error {TraceId}", context.TraceIdentifier);
            await WriteAsync(context, 500, ErrorCodes.Internal, "An unexpected error occurred.");
        }
    }

    public static Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            new { type = "about:blank", title = message, status, code, message, traceId = context.TraceIdentifier },
            (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
