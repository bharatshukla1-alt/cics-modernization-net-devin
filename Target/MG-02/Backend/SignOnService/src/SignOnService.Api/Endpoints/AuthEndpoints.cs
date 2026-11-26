using SignOnService.Api.Auth;

namespace SignOnService.Api.Endpoints;

/// <summary>Sign-in (COSGN00C PROCESS-ENTER-KEY / READ-USER-SEC-FILE).</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/auth").WithTags("Auth")
            .MapPost("/sign-in", async (SignInRequest request, SignInService service, CancellationToken ct) =>
                Results.Ok(await service.SignInAsync(request, ct)))
            .AllowAnonymous()
            .RequireRateLimiting("sign-in")
            .WithName("SignIn")
            .WithSummary("Verify a user ID and password against usrsec and issue a bearer token.")
            .Produces<SignInResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
        return app;
    }
}
