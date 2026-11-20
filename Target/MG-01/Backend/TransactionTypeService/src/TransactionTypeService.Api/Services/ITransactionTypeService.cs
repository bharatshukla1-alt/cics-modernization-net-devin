using TransactionTypeService.Api.Dtos;

namespace TransactionTypeService.Api.Services;

/// <summary>Transaction type business operations. Failures are raised as <see cref="Errors.ServiceException"/>.</summary>
public interface ITransactionTypeService
{
    /// <summary>Returns one keyset page that matches the filters.</summary>
    Task<TransactionTypePageDto> ListAsync(ListQuery query, CancellationToken ct);

    /// <summary>Returns one transaction type or throws NotFound.</summary>
    Task<TransactionTypeDto> GetAsync(string typeCode, CancellationToken ct);

    /// <summary>Creates a transaction type; throws Conflict when the code exists.</summary>
    Task<TransactionTypeDto> CreateAsync(CreateTransactionTypeRequest request, string user, CancellationToken ct);

    /// <summary>Changes a description under a row lock; throws NotFound or NoChanges.</summary>
    Task<TransactionTypeDto> UpdateAsync(string typeCode, UpdateTransactionTypeRequest request, string user, CancellationToken ct);

    /// <summary>Deletes a transaction type; throws NotFound when absent.</summary>
    Task DeleteAsync(string typeCode, string user, CancellationToken ct);
}
