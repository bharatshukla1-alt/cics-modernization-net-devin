namespace TransactionTypeService.Api.Domain;

/// <summary>Transaction type reference row (TRANSACTION_TYPE).</summary>
public class TransactionType
{
    public string TypeCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
