using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TransactionTypeService.Api.Security;

/// <summary>Local-development only: accepts "Bearer dummy-dev-token" when no OAuth provider is available.</summary>
public class DevBypassAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevBypass";
    public const string Token = "dummy-dev-token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.Authorization.ToString() != $"Bearer {Token}")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "dev-admin"),
            new Claim(ClaimTypes.Name, "dev-admin"),
            new Claim(ClaimTypes.Role, AuthorizationPolicies.AdminRole),
            new Claim("scope", $"{AuthorizationPolicies.ReadScope} {AuthorizationPolicies.WriteScope}")
        ], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
