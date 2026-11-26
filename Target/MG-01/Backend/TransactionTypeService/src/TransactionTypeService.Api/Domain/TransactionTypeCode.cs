namespace TransactionTypeService.Api.Domain;

/// <summary>Two-digit, non-zero transaction type code (legacy 1245-EDIT-NUM-REQD / 1210-EDIT-TRANTYPE).</summary>
public static class TransactionTypeCode
{
    public const string Required = "Type code is required.";
    public const string NotNumeric = "Type code must be numeric.";
    public const string Zero = "Type code must not be zero.";
    public const string FilterInvalid = "Type code must be a 2-digit number.";

    public static bool TryNormalize(string? raw, out string code, out string? error)
    {
        code = string.Empty;
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            error = Required;
            return false;
        }
        if (value.Length > 2 || !value.All(char.IsAsciiDigit))
        {
            error = NotNumeric;
            return false;
        }
        var number = int.Parse(value);
        if (number == 0)
        {
            error = Zero;
            return false;
        }
        code = number.ToString("D2");
        error = null;
        return true;
    }

    /// <summary>List filter: blank or zeros means "no filter" (legacy 1220-EDIT-TYPECD).</summary>
    public static bool TryNormalizeFilter(string? raw, out string? code)
    {
        code = null;
        var value = raw?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return true;
        }
        if (value.Length > 2 || !value.All(char.IsAsciiDigit))
        {
            return false;
        }
        var number = int.Parse(value);
        code = number == 0 ? null : number.ToString("D2");
        return true;
    }

    public static bool IsPagingKey(string? raw) =>
        raw is { Length: 2 } && raw.All(char.IsAsciiDigit);
}
