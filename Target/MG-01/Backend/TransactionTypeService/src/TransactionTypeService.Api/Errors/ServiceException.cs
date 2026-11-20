namespace TransactionTypeService.Api.Errors;

/// <summary>Category of a business-rule failure. Mapped to an HTTP status only at the API edge.</summary>
public enum ErrorKind
{
    /// <summary>Input failed validation.</summary>
    Validation,
    /// <summary>The requested transaction type does not exist.</summary>
    NotFound,
    /// <summary>The change conflicts with existing data.</summary>
    Conflict,
    /// <summary>The submitted values equal the stored values.</summary>
    NoChanges
}

/// <summary>Transport-agnostic business-rule failure raised by validators and services.</summary>
public class ServiceException(ErrorKind kind, string code, string message, IDictionary<string, string[]>? errors = null)
    : Exception(message)
{
    /// <summary>Failure category.</summary>
    public ErrorKind Kind { get; } = kind;

    /// <summary>Stable machine-readable code from <see cref="ErrorCodes"/>.</summary>
    public string Code { get; } = code;

    /// <summary>Field-level messages keyed by request property name.</summary>
    public IDictionary<string, string[]>? Errors { get; } = errors;

    /// <summary>Creates a field validation failure.</summary>
    public static ServiceException Validation(IDictionary<string, string[]> errors) =>
        new(ErrorKind.Validation, ErrorCodes.ValidationFailed, "One or more fields are invalid.", errors);
}
