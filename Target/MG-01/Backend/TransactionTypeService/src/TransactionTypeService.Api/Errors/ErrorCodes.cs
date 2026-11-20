namespace TransactionTypeService.Api.Errors;

/// <summary>Stable machine-readable error codes returned in the ProblemDetails <c>code</c> member.</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string TypeFilterInvalid = "TYPE_FILTER_INVALID";
    public const string TypeNotFound = "TYPE_NOT_FOUND";
    public const string TypeAlreadyExists = "TYPE_ALREADY_EXISTS";
    public const string NoChanges = "NO_CHANGES";
    public const string RecordLocked = "RECORD_LOCKED";
    public const string HasDependentRecords = "HAS_DEPENDENT_RECORDS";
    public const string DbUnavailable = "DB_UNAVAILABLE";
    public const string InternalError = "INTERNAL_ERROR";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
}
