namespace TransactionTypeService.Api.Dtos;

/// <summary>A transaction type: 2-digit code and description (TRANSACTION_TYPE row).</summary>
/// <param name="TypeCode">Two-digit code, 01-99.</param>
/// <param name="Description">Letters, numbers and spaces, at most 50 characters.</param>
public record TransactionTypeDto(string TypeCode, string Description);

/// <summary>Request body for creating a transaction type.</summary>
/// <param name="TypeCode">Code 1-99; a single digit is normalised to two digits.</param>
/// <param name="Description">Letters, numbers and spaces, at most 50 characters.</param>
public record CreateTransactionTypeRequest(string? TypeCode, string? Description);

/// <summary>Request body for changing a transaction type description.</summary>
/// <param name="Description">Letters, numbers and spaces, at most 50 characters.</param>
public record UpdateTransactionTypeRequest(string? Description);

/// <summary>One keyset page of transaction types.</summary>
/// <param name="Items">Rows on this page, ordered by code.</param>
/// <param name="FirstCode">Code of the first row; pass as <c>before</c> to page backward.</param>
/// <param name="LastCode">Code of the last row; pass as <c>after</c> to page forward.</param>
/// <param name="HasNext">True when more rows follow.</param>
/// <param name="HasPrevious">True when rows precede this page.</param>
/// <param name="Filtered">True when a type or description filter was applied.</param>
public record TransactionTypePageDto(
    IReadOnlyList<TransactionTypeDto> Items,
    string? FirstCode,
    string? LastCode,
    bool HasNext,
    bool HasPrevious,
    bool Filtered);

/// <summary>Query parameters for listing transaction types.</summary>
public class ListQuery
{
    /// <summary>Exact type code filter (1-2 digits); blank or 0 means no filter.</summary>
    public string? TypeCode { get; set; }

    /// <summary>Case-sensitive "contains" filter on the description.</summary>
    public string? Description { get; set; }

    /// <summary>Return rows with a code greater than this key (next page).</summary>
    public string? After { get; set; }

    /// <summary>Return rows with a code less than this key (previous page).</summary>
    public string? Before { get; set; }

    /// <summary>Rows per page, 1-50; default 7.</summary>
    public int? PageSize { get; set; }
}
