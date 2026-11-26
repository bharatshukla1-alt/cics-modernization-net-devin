using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SignOnService.Api.Errors;

namespace SignOnService.Api.Security;

/// <summary>Returns 401/403 as ProblemDetails with machine codes.</summary>
internal sealed class ProblemAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Challenged)
        {
            context.Response.Headers.WWWAuthenticate = "Bearer";
            return ProblemDetailsMiddleware.WriteAsync(context, 401, ErrorCodes.Unauthenticated, "Sign in to continue.");
        }
        if (result.Forbidden)
        {
            return ProblemDetailsMiddleware.WriteAsync(context, 403, ErrorCodes.Forbidden, "This menu is not available for your role.");
        }
        return _default.HandleAsync(next, context, policy, result);
    }
}
