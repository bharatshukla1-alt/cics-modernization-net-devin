using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TransactionTypeService.Api.Security;

public static class SecurityConfig
{
    public static IServiceCollection AddSecurityConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Authentication:Authority"];
                options.Audience = configuration["Authentication:Audience"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

        var adminAuthority = configuration["Authentication:AdminAuthority"] ?? "TXN_TYPE_ADMIN";
        var readAuthority = configuration["Authentication:ReadAuthority"] ?? "TXN_TYPE_READ";

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminAccess", policy => policy.RequireAssertion(ctx => HasAuthority(ctx.User, adminAuthority)));
            options.AddPolicy("ReadAccess", policy => policy.RequireAssertion(ctx =>
                HasAuthority(ctx.User, readAuthority) || HasAuthority(ctx.User, adminAuthority)));
        });

        return services;
    }

    // Keycloak issues scopes as one space-separated "scope" claim and realm roles as role claims.
    private static bool HasAuthority(ClaimsPrincipal user, string authority) =>
        user.FindAll("scope").Concat(user.FindAll("scp"))
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Concat(user.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Any(v => string.Equals(v, authority, StringComparison.OrdinalIgnoreCase));
}
