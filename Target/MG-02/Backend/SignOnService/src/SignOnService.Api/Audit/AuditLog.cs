namespace SignOnService.Api.Audit;

public sealed record AuditEvent(string Action, string User, string Outcome, string? Reason, string? ClientIp, string TraceId);

public interface IAuditLog
{
    void Write(string action, string user, string outcome, string? reason = null);
}

/// <summary>Structured audit events. Passwords are never part of an event.</summary>
public sealed class AuditLog(ILogger<AuditLog> logger, IHttpContextAccessor accessor) : IAuditLog
{
    public void Write(string action, string user, string outcome, string? reason = null)
    {
        var context = accessor.HttpContext;
        var evt = new AuditEvent(action, user, outcome, reason, context?.Connection.RemoteIpAddress?.ToString(), context?.TraceIdentifier ?? "");
        logger.LogInformation("AUDIT {@Audit}", evt);
    }
}
