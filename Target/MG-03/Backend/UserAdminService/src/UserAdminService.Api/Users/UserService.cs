using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserAdminService.Api.Audit;
using UserAdminService.Api.Data;
using UserAdminService.Api.Errors;

namespace UserAdminService.Api.Users;

/// <summary>COUSR00C–COUSR03C operations on usrsec.</summary>
public sealed class UserService(UserAdminDbContext db, IAuditLog audit, ILogger<UserService> logger)
{
    public const int PageSize = 10;
    public const string TopMessage = "You are already at the top of the page...";
    public const string BottomMessage = "You are already at the bottom of the page...";

    /// <summary>PROCESS-PAGE-FORWARD / PROCESS-PAGE-BACKWARD with keyset paging on sec_usr_id.</summary>
    public async Task<UserPage> ListAsync(string? search, string? after, string? before, CancellationToken ct)
    {
        if (new[] { search, after, before }.Count(v => !string.IsNullOrWhiteSpace(v)) > 1)
            throw new ApiException(400, ErrorCodes.InvalidQuery, "Use only one of search, after or before.");
        try
        {
            var users = db.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(before))
            {
                var key = Key(before);
                var rows = await users.Where(u => string.Compare(u.UserId, key) < 0)
                    .OrderByDescending(u => u.UserId).Take(PageSize + 1).ToListAsync(ct);
                if (rows.Count == 0) return Empty(TopMessage, hasNext: true);
                var page = rows.Take(PageSize).Reverse().ToList();
                return Page(page, hasPrevious: rows.Count > PageSize, hasNext: true);
            }
            var forward = !string.IsNullOrWhiteSpace(after);
            var start = forward ? Key(after!) : Key(search ?? "");
            var query = forward
                ? users.Where(u => string.Compare(u.UserId, start) > 0)
                : users.Where(u => string.Compare(u.UserId, start) >= 0);
            var list = await query.OrderBy(u => u.UserId).Take(PageSize + 1).ToListAsync(ct);
            if (list.Count == 0 && forward) return Empty(BottomMessage, hasPrevious: true);
            var shown = list.Take(PageSize).ToList();
            var hasPrevious = forward || (shown.Count > 0 && await users.AnyAsync(u => string.Compare(u.UserId, shown[0].UserId) < 0, ct));
            return Page(shown, hasPrevious, hasNext: list.Count > PageSize);
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            logger.LogError(ex, "User list failed");
            throw new ApiException(503, ErrorCodes.UnableToLookup, "Unable to lookup User...");
        }
    }

    public async Task<UserDetail> GetAsync(string userId, CancellationToken ct)
    {
        var id = UserValidator.NormalizeUserId(userId);
        try
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id, ct);
            return user?.ToDetail() ?? throw NotFound();
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            logger.LogError(ex, "User lookup failed");
            throw new ApiException(503, ErrorCodes.UnableToLookup, "Unable to lookup User...");
        }
    }

    /// <summary>COUSR01C WRITE-USER-SEC-FILE.</summary>
    public async Task<UserResult> CreateAsync(CreateUserRequest request, string actor, CancellationToken ct)
    {
        var v = UserValidator.ValidateCreate(request);
        var user = new SecurityUser { UserId = v.UserId, FirstName = v.FirstName, LastName = v.LastName, Password = v.Password!, UserType = v.UserType };
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            audit.Write("user.create", actor, "failure", ErrorCodes.UserIdExists, v.UserId);
            throw new ApiException(409, ErrorCodes.UserIdExists, "User ID already exist...");
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            logger.LogError(ex, "User add failed");
            audit.Write("user.create", actor, "failure", ErrorCodes.UnableToAdd, v.UserId);
            throw new ApiException(503, ErrorCodes.UnableToAdd, "Unable to Add User...");
        }
        audit.Write("user.create", actor, "success", null, v.UserId);
        return new(user.ToDetail(), $"User {v.UserId} has been added ...");
    }

    /// <summary>COUSR02C UPDATE-USER-INFO / REWRITE, with optimistic concurrency and admin protection.</summary>
    public async Task<UserResult> UpdateAsync(string userId, UpdateUserRequest request, uint? expectedVersion, string actor, CancellationToken ct)
    {
        var v = UserValidator.ValidateUpdate(userId, request);
        var version = expectedVersion ?? throw VersionRequired();
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == v.UserId, ct) ?? throw NotFound();
            if (user.Version != version) throw Stale();
            var current = user.ToDetail();
            var unchanged = current.FirstName == v.FirstName && current.LastName == v.LastName && current.UserType == v.UserType
                            && (v.Password is null || v.Password == user.Password.TrimEnd());
            if (unchanged) throw new ApiException(400, ErrorCodes.NoChanges, "Please modify to update ...");
            if (current.UserType == "A" && v.UserType != "A")
            {
                if (string.Equals(v.UserId, actor, StringComparison.OrdinalIgnoreCase))
                    throw new ApiException(409, ErrorCodes.CannotDemoteSelf, "You cannot remove your own administrator access...");
                await EnsureAnotherAdminAsync(ct);
            }
            user.FirstName = v.FirstName;
            user.LastName = v.LastName;
            user.UserType = v.UserType;
            if (v.Password is not null) user.Password = v.Password;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            audit.Write("user.update", actor, "success", null, v.UserId);
            return new(user.ToDetail(), $"User {v.UserId} has been updated ...");
        }
        catch (ApiException ex)
        {
            audit.Write("user.update", actor, "failure", ex.Code, v.UserId);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            audit.Write("user.update", actor, "failure", ErrorCodes.StaleUser, v.UserId);
            throw Stale();
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            logger.LogError(ex, "User update failed");
            audit.Write("user.update", actor, "failure", ErrorCodes.UnableToUpdate, v.UserId);
            throw new ApiException(503, ErrorCodes.UnableToUpdate, "Unable to Update User...");
        }
    }

    /// <summary>COUSR03C DELETE-USER-SEC-FILE, with optimistic concurrency and admin protection.</summary>
    public async Task<MessageResult> DeleteAsync(string userId, uint? expectedVersion, string actor, CancellationToken ct)
    {
        var id = UserValidator.NormalizeUserId(userId);
        var version = expectedVersion ?? throw VersionRequired();
        try
        {
            if (string.Equals(id, actor, StringComparison.OrdinalIgnoreCase))
                throw new ApiException(409, ErrorCodes.CannotDeleteSelf, "You cannot delete your own user ID...");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == id, ct) ?? throw NotFound();
            if (user.Version != version) throw Stale();
            if (user.UserType.Trim() == "A") await EnsureAnotherAdminAsync(ct);
            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            audit.Write("user.delete", actor, "success", null, id);
            return new($"User {id} has been deleted ...");
        }
        catch (ApiException ex)
        {
            audit.Write("user.delete", actor, "failure", ex.Code, id);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            audit.Write("user.delete", actor, "failure", ErrorCodes.StaleUser, id);
            throw Stale();
        }
        catch (Exception ex) when (IsDataError(ex))
        {
            logger.LogError(ex, "User delete failed");
            audit.Write("user.delete", actor, "failure", ErrorCodes.UnableToDelete, id);
            throw new ApiException(503, ErrorCodes.UnableToDelete, "Unable to Delete User...");
        }
    }

    /// <summary>Locks the admin rows so two concurrent removals cannot leave zero administrators.</summary>
    private async Task EnsureAnotherAdminAsync(CancellationToken ct)
    {
        var admins = await db.Database
            .SqlQueryRaw<string>("SELECT sec_usr_id AS \"Value\" FROM usrsec WHERE sec_usr_type = 'A' FOR UPDATE")
            .ToListAsync(ct);
        if (!AdminGuard.CanRemoveAdmin(admins.Count))
            throw new ApiException(409, ErrorCodes.LastAdmin, "At least one administrator must remain...");
    }

    private static string Key(string value) => value.Trim().ToUpperInvariant();

    private static UserPage Page(List<SecurityUser> rows, bool hasPrevious, bool hasNext) =>
        new(rows.Select(u => u.ToSummary()).ToList(), rows.FirstOrDefault()?.UserId.TrimEnd(), rows.LastOrDefault()?.UserId.TrimEnd(), hasPrevious, hasNext, null);

    private static UserPage Empty(string message, bool hasPrevious = false, bool hasNext = false) =>
        new([], null, null, hasPrevious, hasNext, message);

    private static bool IsDataError(Exception ex) => ex is NpgsqlException or DbUpdateException or InvalidOperationException { InnerException: NpgsqlException };

    private static ApiException NotFound() => new(404, ErrorCodes.UserNotFound, "User ID NOT found...");
    private static ApiException Stale() => new(412, ErrorCodes.StaleUser, "This user was changed by someone else. Fetch it again...");
    private static ApiException VersionRequired() => new(428, ErrorCodes.VersionRequired, "Fetch the user first: the If-Match version is required.");
}

public static class AdminGuard
{
    /// <summary>An administrator can be removed or demoted only while another one remains.</summary>
    public static bool CanRemoveAdmin(int adminCount) => adminCount > 1;
}
