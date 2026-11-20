using System.Text.RegularExpressions;
using TransactionTypeService.Api.Domain;
using TransactionTypeService.Api.Dtos;
using TransactionTypeService.Api.Errors;

namespace TransactionTypeService.Api.Validation;

public static partial class TransactionTypeValidators
{
    public const int DefaultPageSize = 7;
    public const int MaxDescriptionLength = 50;

    [GeneratedRegex("^[A-Za-z0-9 ]+$")]
    private static partial Regex AlphanumericWithSpaces();

    /// <summary>Legacy 1240-EDIT-ALPHANUM-REQD / 1230-EDIT-ALPHANUM-REQD.</summary>
    public static string? DescriptionError(string? description)
    {
        var value = description?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return "Description is required.";
        }
        if (value.Length > MaxDescriptionLength)
        {
            return "Description can be at most 50 characters.";
        }
        return AlphanumericWithSpaces().IsMatch(value) ? null : "Description can contain only letters, numbers and spaces.";
    }

    public static string NormalizeCodeOrThrow(string? raw)
    {
        if (!TransactionTypeCode.TryNormalize(raw, out var code, out var error))
        {
            throw ServiceException.Validation(new Dictionary<string, string[]> { ["typeCode"] = [error!] });
        }
        return code;
    }

    public static (string Code, string Description) ValidateCreate(CreateTransactionTypeRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!TransactionTypeCode.TryNormalize(request?.TypeCode, out var code, out var codeError))
        {
            errors["typeCode"] = [codeError!];
        }
        if (DescriptionError(request?.Description) is { } descriptionError)
        {
            errors["description"] = [descriptionError];
        }
        if (errors.Count > 0)
        {
            throw ServiceException.Validation(errors);
        }
        return (code, request!.Description!.Trim());
    }

    public static string ValidateUpdate(UpdateTransactionTypeRequest? request)
    {
        if (DescriptionError(request?.Description) is { } error)
        {
            throw ServiceException.Validation(new Dictionary<string, string[]> { ["description"] = [error] });
        }
        return request!.Description!.Trim();
    }

    public record ListCriteria(string? TypeCode, string? DescriptionContains, string? After, string? Before, int PageSize)
    {
        public bool Filtered => TypeCode is not null || DescriptionContains is not null;
    }

    public static ListCriteria ValidateList(ListQuery query)
    {
        if (!TransactionTypeCode.TryNormalizeFilter(query.TypeCode, out var typeCode))
        {
            throw new ServiceException(ErrorKind.Validation, ErrorCodes.TypeFilterInvalid,
                TransactionTypeCode.FilterInvalid,
                new Dictionary<string, string[]> { ["typeCode"] = [TransactionTypeCode.FilterInvalid] });
        }

        var errors = new Dictionary<string, string[]>();
        var description = query.Description?.Trim();
        if (description is { Length: > MaxDescriptionLength })
        {
            errors["description"] = ["Description filter can be at most 50 characters."];
        }
        if (query.After is not null && query.Before is not null)
        {
            errors["after"] = ["Use either after or before, not both."];
        }
        else
        {
            if (query.After is not null && !TransactionTypeCode.IsPagingKey(query.After))
            {
                errors["after"] = ["Paging key must be a 2-digit code."];
            }
            if (query.Before is not null && !TransactionTypeCode.IsPagingKey(query.Before))
            {
                errors["before"] = ["Paging key must be a 2-digit code."];
            }
        }
        var pageSize = query.PageSize ?? DefaultPageSize;
        if (pageSize is < 1 or > 50)
        {
            errors["pageSize"] = ["Page size must be between 1 and 50."];
        }
        if (errors.Count > 0)
        {
            throw ServiceException.Validation(errors);
        }

        return new ListCriteria(typeCode, string.IsNullOrEmpty(description) ? null : description,
            query.After, query.Before, pageSize);
    }
}
