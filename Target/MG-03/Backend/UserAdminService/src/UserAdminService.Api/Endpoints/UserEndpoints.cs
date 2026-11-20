using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using UserAdminService.Api.Data;
using UserAdminService.Api.Security;
using UserAdminService.Api.Users;

namespace UserAdminService.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/users").WithTags("Users");

        users.MapGet("/", (string? search, string? after, string? before, UserService svc, CancellationToken ct) =>
                svc.ListAsync(search, after, before, ct))
            .RequireAuthorization(Policies.UsersRead).WithName("ListUsers")
            .WithSummary("List users, 10 per page in user-ID order (COUSR00C).");

        users.MapGet("/{userId}", async (string userId, UserService svc, HttpContext http, CancellationToken ct) =>
            {
                var user = await svc.GetAsync(userId, ct);
                http.Response.Headers.ETag = $"\"{user.Version}\"";
                return user;
            })
            .RequireAuthorization(Policies.UsersRead).WithName("GetUser")
            .WithSummary("Fetch one user without the password (COUSR02C/COUSR03C READ).");

        users.MapPost("/", async (CreateUserRequest body, UserService svc, ClaimsPrincipal principal, CancellationToken ct) =>
            {
                var result = await svc.CreateAsync(body, Actor(principal), ct);
                return TypedResults.Created($"/api/v1/users/{result.User.UserId}", result);
            })
            .RequireAuthorization(Policies.UsersWrite).WithName("CreateUser")
            .WithSummary("Add a user (COUSR01C WRITE).");

        users.MapPut("/{userId}", (string userId, UpdateUserRequest body, UserService svc, HttpContext http, ClaimsPrincipal principal, CancellationToken ct) =>
                svc.UpdateAsync(userId, body, IfMatch(http), Actor(principal), ct))
            .RequireAuthorization(Policies.UsersWrite).WithName("UpdateUser")
            .WithSummary("Update a user; requires If-Match (COUSR02C REWRITE).");

        users.MapDelete("/{userId}", (string userId, UserService svc, HttpContext http, ClaimsPrincipal principal, CancellationToken ct) =>
                svc.DeleteAsync(userId, IfMatch(http), Actor(principal), ct))
            .RequireAuthorization(Policies.UsersWrite).WithName("DeleteUser")
            .WithSummary("Delete a user; requires If-Match (COUSR03C DELETE).");

        return app;
    }

    public static IEndpointRouteBuilder MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (UserAdminDbContext db, CancellationToken ct) =>
            await db.Database.CanConnectAsync(ct)
                ? Results.Ok(new { status = "Healthy" })
                : Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable))
            .AllowAnonymous().WithName("Health");
        return app;
    }

    public static uint? IfMatch(HttpContext http)
    {
        var raw = http.Request.Headers.IfMatch.ToString().Trim();
        if (raw.StartsWith("W/", StringComparison.Ordinal)) raw = raw[2..];
        return uint.TryParse(raw.Trim('"'), out var version) ? version : null;
    }

    private static string Actor(ClaimsPrincipal principal) => principal.FindFirst("sub")?.Value ?? "unknown";
}
