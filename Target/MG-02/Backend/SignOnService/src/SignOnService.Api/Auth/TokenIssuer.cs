using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SignOnService.Api.Data;

namespace SignOnService.Api.Auth;

public static class Roles
{
    public const string Admin = "carddemo.admin";
    public const string User = "carddemo.user";
    public const string AdminScopes = "transaction-types.read transaction-types.write users.read users.write";
}

/// <summary>Issues the bearer token that replaces the COMMAREA user fields (CDEMO-USER-ID/-TYPE).</summary>
public sealed class TokenIssuer(SigningKeys keys, AuthOptions options, TimeProvider time)
{
    public (string Token, int ExpiresIn) Issue(SecurityUser user)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var isAdmin = user.UserType == "A";
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.UserId,
            ["role"] = isAdmin ? Roles.Admin : Roles.User,
            ["user_type"] = user.UserType
        };
        if (isAdmin) claims["scope"] = Roles.AdminScopes;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(options.TokenMinutes),
            SigningCredentials = new SigningCredentials(keys.Key, SecurityAlgorithms.RsaSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), options.TokenMinutes * 60);
    }
}
