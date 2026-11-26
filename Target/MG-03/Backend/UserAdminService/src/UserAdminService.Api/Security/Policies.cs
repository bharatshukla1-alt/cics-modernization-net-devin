using System.Security.Claims;

namespace UserAdminService.Api.Security;

public static class Policies
{
    public const string AdminRole = "carddemo.admin";
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";

    public static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindAll("scope").SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Contains(scope);
}
