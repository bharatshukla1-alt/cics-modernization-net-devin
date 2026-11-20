using Npgsql;
using TransactionTypeService.Api.Errors;

namespace TransactionTypeService.Api.Audit;

/// <summary>Audit action names for transaction type changes.</summary>
public static class AuditActions
{
    /// <summary>Transaction type created.</summary>
    public const string Create = "CREATE";
    /// <summary>Transaction type description changed.</summary>
    public const string Update = "UPDATE";
    /// <summary>Transaction type deleted.</summary>
    public const string Delete = "DELETE";
}

/// <summary>One audited change attempt, with before/after values and outcome (PCI-DSS 10.2).</summary>
public record AuditEvent(string User, string Action, string TypeCode, string? Before, string? After, string Outcome)
{
    /// <summary>Outcome value for a committed change.</summary>
    public const string Success = "SUCCESS";

    /// <summary>Outcome value for a rejected or failed change, carrying the error code or SQLSTATE.</summary>
    public static string Failed(Exception ex) => ex switch
    {
        ServiceException s => $"FAILED:{s.Code}",
        { InnerException: PostgresException p } => $"FAILED:{p.SqlState}",
        PostgresException p => $"FAILED:{p.SqlState}",
        _ => $"FAILED:{ex.GetType().Name}"
    };
}

/// <summary>Sink for audit events.</summary>
public interface IAuditLog
{
    /// <summary>Records one audit event.</summary>
    void Record(AuditEvent auditEvent);
}

/// <summary>
/// Writes structured audit events to the dedicated <c>Audit</c> log category (event id 9000) so the
/// deployment can route them to an append-only store or SIEM.
/// </summary>
public sealed class AuditLog(ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor) : IAuditLog
{
    private static readonly EventId AuditEventId = new(9000, "Audit");
    private readonly ILogger _logger = loggerFactory.CreateLogger("Audit");

    /// <inheritdoc />
    public void Record(AuditEvent auditEvent) =>
        _logger.LogInformation(AuditEventId,
            "Audit {Action} transaction type {TypeCode} by {User}: {Outcome} (before: {Before}, after: {After}, trace: {TraceId})",
            auditEvent.Action, auditEvent.TypeCode, auditEvent.User, auditEvent.Outcome, auditEvent.Before, auditEvent.After,
            httpContextAccessor.HttpContext?.TraceIdentifier);
}
