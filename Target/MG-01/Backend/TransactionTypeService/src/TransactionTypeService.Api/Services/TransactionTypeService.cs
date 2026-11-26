using Microsoft.EntityFrameworkCore;
using Npgsql;
using TransactionTypeService.Api.Audit;
using TransactionTypeService.Api.Domain;
using TransactionTypeService.Api.Dtos;
using TransactionTypeService.Api.Errors;
using TransactionTypeService.Api.Repositories;
using TransactionTypeService.Api.Validation;

namespace TransactionTypeService.Api.Services;

/// <summary>Business rules of legacy COTRTLIC (list/filter/inline update/delete) and COTRTUPC (lookup/add/update/delete).</summary>
public class TransactionTypeService(ITransactionTypeRepository repository, IAuditLog audit) : ITransactionTypeService
{
    /// <inheritdoc />
    public async Task<TransactionTypePageDto> ListAsync(ListQuery query, CancellationToken ct)
    {
        var criteria = TransactionTypeValidators.ValidateList(query);
        List<TransactionType> rows;
        bool hasNext;

        if (criteria.Before is not null)
        {
            rows = await repository.GetBackwardAsync(criteria.TypeCode, criteria.DescriptionContains, criteria.Before, criteria.PageSize, ct);
            hasNext = true;
            if (rows.Count == 0)
            {
                (rows, hasNext) = await ForwardPageAsync(criteria, after: null, ct);
            }
        }
        else
        {
            (rows, hasNext) = await ForwardPageAsync(criteria, criteria.After, ct);
        }

        var hasPrevious = rows.Count > 0 &&
                          await repository.ExistsBeforeAsync(criteria.TypeCode, criteria.DescriptionContains, rows[0].TypeCode, ct);

        return new TransactionTypePageDto(
            rows.Select(ToDto).ToList(),
            rows.FirstOrDefault()?.TypeCode,
            rows.LastOrDefault()?.TypeCode,
            hasNext,
            hasPrevious,
            criteria.Filtered);
    }

    /// <inheritdoc />
    public async Task<TransactionTypeDto> GetAsync(string typeCode, CancellationToken ct)
    {
        var code = TransactionTypeValidators.NormalizeCodeOrThrow(typeCode);
        var entity = await repository.FindAsync(code, ct)
                     ?? throw new ServiceException(ErrorKind.NotFound, ErrorCodes.TypeNotFound, $"No transaction type with code {code}.");
        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<TransactionTypeDto> CreateAsync(CreateTransactionTypeRequest request, string user, CancellationToken ct)
    {
        var (code, description) = TransactionTypeValidators.ValidateCreate(request);
        try
        {
            repository.Add(new TransactionType { TypeCode = code, Description = description });
            try
            {
                await repository.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new ServiceException(ErrorKind.Conflict, ErrorCodes.TypeAlreadyExists,
                    $"Transaction type {code} already exists.",
                    new Dictionary<string, string[]> { ["typeCode"] = [$"Transaction type {code} already exists."] });
            }
            audit.Record(new AuditEvent(user, AuditActions.Create, code, null, description, AuditEvent.Success));
            return new TransactionTypeDto(code, description);
        }
        catch (Exception ex)
        {
            audit.Record(new AuditEvent(user, AuditActions.Create, code, null, description, AuditEvent.Failed(ex)));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TransactionTypeDto> UpdateAsync(string typeCode, UpdateTransactionTypeRequest request, string user, CancellationToken ct)
    {
        var code = TransactionTypeValidators.NormalizeCodeOrThrow(typeCode);
        var description = TransactionTypeValidators.ValidateUpdate(request);
        string? before = null;
        try
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            var entity = await repository.FindForUpdateAsync(code, ct)
                         ?? throw NotFoundForChange(code);
            before = entity.Description;

            if (string.Equals(entity.Description.Trim(), description, StringComparison.OrdinalIgnoreCase))
            {
                throw new ServiceException(ErrorKind.NoChanges, ErrorCodes.NoChanges,
                    "No change detected with respect to database values.");
            }

            entity.Description = description;
            await repository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            audit.Record(new AuditEvent(user, AuditActions.Update, code, before, description, AuditEvent.Success));
            return ToDto(entity);
        }
        catch (Exception ex)
        {
            audit.Record(new AuditEvent(user, AuditActions.Update, code, before, description, AuditEvent.Failed(ex)));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string typeCode, string user, CancellationToken ct)
    {
        var code = TransactionTypeValidators.NormalizeCodeOrThrow(typeCode);
        string? before = null;
        try
        {
            before = (await repository.FindAsync(code, ct))?.Description;
            var deleted = await repository.DeleteAsync(code, ct);
            if (deleted == 0)
            {
                throw NotFoundForChange(code);
            }
            audit.Record(new AuditEvent(user, AuditActions.Delete, code, before, null, AuditEvent.Success));
        }
        catch (Exception ex)
        {
            audit.Record(new AuditEvent(user, AuditActions.Delete, code, before, null, AuditEvent.Failed(ex)));
            throw;
        }
    }

    private async Task<(List<TransactionType> Rows, bool HasNext)> ForwardPageAsync(
        TransactionTypeValidators.ListCriteria criteria, string? after, CancellationToken ct)
    {
        var rows = await repository.GetForwardAsync(criteria.TypeCode, criteria.DescriptionContains, after, criteria.PageSize + 1, ct);
        var hasNext = rows.Count > criteria.PageSize;
        if (hasNext)
        {
            rows.RemoveAt(rows.Count - 1);
        }
        return (rows, hasNext);
    }

    private static ServiceException NotFoundForChange(string code) =>
        new(ErrorKind.NotFound, ErrorCodes.TypeNotFound,
            $"Transaction type {code} was not found. It may have been deleted by someone else.");

    private static TransactionTypeDto ToDto(TransactionType t) => new(t.TypeCode, t.Description);
}
