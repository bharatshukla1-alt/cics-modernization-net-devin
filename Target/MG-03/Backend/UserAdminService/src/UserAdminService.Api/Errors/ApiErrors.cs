namespace UserAdminService.Api.Errors;

public static class ErrorCodes
{
    public const string FirstNameRequired = "FIRST_NAME_REQUIRED";
    public const string LastNameRequired = "LAST_NAME_REQUIRED";
    public const string UserIdRequired = "USER_ID_REQUIRED";
    public const string PasswordRequired = "PASSWORD_REQUIRED";
    public const string UserTypeRequired = "USER_TYPE_REQUIRED";
    public const string InvalidUserType = "INVALID_USER_TYPE";
    public const string InvalidUserId = "INVALID_USER_ID";
    public const string FieldTooLong = "FIELD_TOO_LONG";
    public const string InvalidQuery = "INVALID_QUERY";
    public const string UserIdExists = "USER_ID_EXISTS";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string NoChanges = "NO_CHANGES";
    public const string CannotDeleteSelf = "CANNOT_DELETE_SELF";
    public const string CannotDemoteSelf = "CANNOT_DEMOTE_SELF";
    public const string LastAdmin = "LAST_ADMIN";
    public const string VersionRequired = "VERSION_REQUIRED";
    public const string StaleUser = "STALE_USER";
    public const string UnableToAdd = "UNABLE_TO_ADD";
    public const string UnableToLookup = "UNABLE_TO_LOOKUP";
    public const string UnableToUpdate = "UNABLE_TO_UPDATE";
    public const string UnableToDelete = "UNABLE_TO_DELETE";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
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
