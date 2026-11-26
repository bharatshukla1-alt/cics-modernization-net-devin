using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TransactionTypeService.Api.Data;
using TransactionTypeService.Api.Domain;

namespace TransactionTypeService.Api.Repositories;

/// <summary>Keyset access to transaction_type, mirroring legacy cursors C-TR-TYPE-FORWARD / C-TR-TYPE-BACKWARD.</summary>
public class TransactionTypeRepository(CardDemoDbContext db) : ITransactionTypeRepository
{
    private const string LikeEscape = "\\";

    public Task<List<TransactionType>> GetForwardAsync(string? typeCode, string? descriptionContains, string? after, int take, CancellationToken ct)
    {
        var query = Filtered(typeCode, descriptionContains);
        if (after is not null)
        {
            query = query.Where(t => string.Compare(t.TypeCode, after) > 0);
        }
        return query.OrderBy(t => t.TypeCode).Take(take).ToListAsync(ct);
    }

    public async Task<List<TransactionType>> GetBackwardAsync(string? typeCode, string? descriptionContains, string before, int take, CancellationToken ct)
    {
        var rows = await Filtered(typeCode, descriptionContains)
            .Where(t => string.Compare(t.TypeCode, before) < 0)
            .OrderByDescending(t => t.TypeCode)
            .Take(take)
            .ToListAsync(ct);
        rows.Reverse();
        return rows;
    }

    public Task<bool> ExistsBeforeAsync(string? typeCode, string? descriptionContains, string firstCode, CancellationToken ct) =>
        Filtered(typeCode, descriptionContains).AnyAsync(t => string.Compare(t.TypeCode, firstCode) < 0, ct);

    public Task<TransactionType?> FindAsync(string typeCode, CancellationToken ct) =>
        db.TransactionTypes.AsNoTracking().FirstOrDefaultAsync(t => t.TypeCode == typeCode, ct);

    public Task<TransactionType?> FindForUpdateAsync(string typeCode, CancellationToken ct) =>
        db.TransactionTypes
            .FromSql($"SELECT tr_type, tr_description FROM transaction_type WHERE tr_type = {typeCode} FOR UPDATE NOWAIT")
            .FirstOrDefaultAsync(ct);

    public void Add(TransactionType transactionType) => db.TransactionTypes.Add(transactionType);

    public Task<int> DeleteAsync(string typeCode, CancellationToken ct) =>
        db.TransactionTypes.Where(t => t.TypeCode == typeCode).ExecuteDeleteAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);

    private IQueryable<TransactionType> Filtered(string? typeCode, string? descriptionContains)
    {
        var query = db.TransactionTypes.AsNoTracking();
        if (typeCode is not null)
        {
            query = query.Where(t => t.TypeCode == typeCode);
        }
        if (descriptionContains is not null)
        {
            var pattern = "%" + EscapeLike(descriptionContains) + "%";
            query = query.Where(t => EF.Functions.Like(t.Description, pattern, LikeEscape));
        }
        return query;
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
