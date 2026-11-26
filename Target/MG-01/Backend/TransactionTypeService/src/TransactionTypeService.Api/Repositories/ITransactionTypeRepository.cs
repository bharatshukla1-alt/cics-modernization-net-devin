using Microsoft.EntityFrameworkCore.Storage;
using TransactionTypeService.Api.Domain;

namespace TransactionTypeService.Api.Repositories;

/// <summary>Data access for the <c>transaction_type</c> table.</summary>
public interface ITransactionTypeRepository
{
    /// <summary>Rows with code greater than <paramref name="after"/> (or from the start), ascending.</summary>
    Task<List<TransactionType>> GetForwardAsync(string? typeCode, string? descriptionContains, string? after, int take, CancellationToken ct);
    /// <summary>Up to <paramref name="take"/> rows with code less than <paramref name="before"/>, returned ascending.</summary>
    Task<List<TransactionType>> GetBackwardAsync(string? typeCode, string? descriptionContains, string before, int take, CancellationToken ct);
    /// <summary>True when a matching row precedes <paramref name="firstCode"/>.</summary>
    Task<bool> ExistsBeforeAsync(string? typeCode, string? descriptionContains, string firstCode, CancellationToken ct);
    /// <summary>Reads one row without tracking.</summary>
    Task<TransactionType?> FindAsync(string typeCode, CancellationToken ct);
    /// <summary>Reads and locks one row (<c>FOR UPDATE NOWAIT</c>); call inside a transaction.</summary>
    Task<TransactionType?> FindForUpdateAsync(string typeCode, CancellationToken ct);
    /// <summary>Stages a new row for insert.</summary>
    void Add(TransactionType transactionType);
    /// <summary>Deletes one row; returns the number of rows removed.</summary>
    Task<int> DeleteAsync(string typeCode, CancellationToken ct);
    /// <summary>Persists staged changes.</summary>
    Task SaveChangesAsync(CancellationToken ct);
    /// <summary>Starts a database transaction.</summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
}
