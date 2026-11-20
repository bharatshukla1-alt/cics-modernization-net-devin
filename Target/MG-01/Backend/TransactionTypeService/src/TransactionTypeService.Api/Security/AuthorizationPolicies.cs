using Microsoft.AspNetCore.Authorization;

namespace TransactionTypeService.Api.Security;

/// <summary>Read/write policies: role <c>carddemo.admin</c> plus the matching OAuth scope.</summary>
public static class AuthorizationPolicies
{
    public const string Read = "TransactionTypesRead";
    public const string Write = "TransactionTypesWrite";
    public const string AdminRole = "carddemo.admin";
    public const string ReadScope = "transaction-types.read";
    public const string WriteScope = "transaction-types.write";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(Read, p => p.RequireAuthenticatedUser().RequireRole(AdminRole)
            .RequireAssertion(c => HasScope(c, ReadScope) || HasScope(c, WriteScope)));
        options.AddPolicy(Write, p => p.RequireAuthenticatedUser().RequireRole(AdminRole)
            .RequireAssertion(c => HasScope(c, WriteScope)));
    }

    private static bool HasScope(AuthorizationHandlerContext context, string scope) =>
        context.User.FindAll("scope").SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope);
}
