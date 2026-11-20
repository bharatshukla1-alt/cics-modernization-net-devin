using SignOnService.Api.Auth;
using SignOnService.Api.Data;

namespace SignOnService.Api.Endpoints;

/// <summary>OIDC discovery, JWKS and health: what other services need to trust this issuer.</summary>
public static class WellKnownEndpoints
{
    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder app, AuthOptions options)
    {
        app.MapGet("/.well-known/openid-configuration", () => Results.Ok(new
        {
            issuer = options.Issuer,
            jwks_uri = $"{options.Issuer.TrimEnd('/')}/.well-known/jwks.json",
            response_types_supported = new[] { "token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        })).AllowAnonymous().WithName("OpenIdConfiguration").WithSummary("OIDC discovery document.");
        app.MapGet("/.well-known/jwks.json", (SigningKeys keys) => Results.Ok(keys.Jwks()))
            .AllowAnonymous().WithName("Jwks").WithSummary("Public signing keys (RS256).");
        return app;
    }

    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (SignOnDbContext db, CancellationToken ct) =>
        {
            var healthy = await db.Database.CanConnectAsync(ct);
            return healthy
                ? Results.Ok(new { status = "Healthy" })
                : Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous().WithName("Health");
        return app;
    }
}
