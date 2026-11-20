using UserAdminService.Api.Data;

namespace UserAdminService.Api.Users;

public sealed record UserSummary(string UserId, string FirstName, string LastName, string UserType);

public sealed record UserDetail(string UserId, string FirstName, string LastName, string UserType, uint Version);

public sealed record UserPage(
    IReadOnlyList<UserSummary> Users, string? FirstUserId, string? LastUserId, bool HasPrevious, bool HasNext, string? Message);

public sealed record CreateUserRequest(string? UserId, string? FirstName, string? LastName, string? Password, string? UserType);

public sealed record UpdateUserRequest(string? FirstName, string? LastName, string? Password, string? UserType);

public sealed record UserResult(UserDetail User, string Message);

public sealed record MessageResult(string Message);

public static class UserMapping
{
    public static UserSummary ToSummary(this SecurityUser u) =>
        new(u.UserId.TrimEnd(), u.FirstName.TrimEnd(), u.LastName.TrimEnd(), u.UserType.Trim());

    public static UserDetail ToDetail(this SecurityUser u) =>
        new(u.UserId.TrimEnd(), u.FirstName.TrimEnd(), u.LastName.TrimEnd(), u.UserType.Trim(), u.Version);
}
