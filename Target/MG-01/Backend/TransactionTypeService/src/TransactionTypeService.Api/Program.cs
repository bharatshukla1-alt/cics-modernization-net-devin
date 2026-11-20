using System.Security.Authentication;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransactionTypeService.Api.Audit;
using TransactionTypeService.Api.Data;
using TransactionTypeService.Api.Errors;
using TransactionTypeService.Api.Repositories;
using TransactionTypeService.Api.Security;
using TransactionTypeService.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
var devBypass = builder.Configuration.GetValue<bool>("Auth:DevBypass");
StartupGuards.Validate(builder.Configuration, builder.Environment);

var connectionString = builder.Configuration.GetConnectionString("CardDemo")
                       ?? throw new InvalidOperationException("Connection string 'CardDemo' is not configured (set ConnectionStrings__CardDemo).");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 4096;
    options.AddServerHeader = false;
    if (!isDevelopment)
    {
        options.ConfigureHttpsDefaults(https => https.SslProtocols = SslProtocols.Tls13);
    }
});

builder.Services.AddDbContext<CardDemoDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<ITransactionTypeRepository, TransactionTypeRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditLog, AuditLog>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ITransactionTypeService, TransactionTypeService.Api.Services.TransactionTypeService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context =>
        new ObjectResult(new
        {
            type = "about:blank",
            title = "Request body is invalid.",
            status = 400,
            code = ErrorCodes.ValidationFailed,
            message = "Request body is invalid.",
            traceId = context.HttpContext.TraceIdentifier
        })
        { StatusCode = 400, ContentTypes = { "application/problem+json" } });

const string SmartScheme = "BearerOrDev";
var auth = builder.Services.AddAuthentication(devBypass ? SmartScheme : JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = builder.Configuration["Auth:Authority"];
        o.Audience = builder.Configuration["Auth:Audience"];
        o.RequireHttpsMetadata = !isDevelopment;
        o.MapInboundClaims = false;
        o.TokenValidationParameters.NameClaimType = "sub";
        o.TokenValidationParameters.RoleClaimType = "role";
        o.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
    });
if (devBypass)
{
    auth.AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>(DevBypassAuthHandler.SchemeName, _ => { })
        .AddPolicyScheme(SmartScheme, SmartScheme, o => o.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString() == $"Bearer {DevBypassAuthHandler.Token}"
                ? DevBypassAuthHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme);
}
builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddCors(o => o.AddPolicy("dev-ui", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .WithMethods("GET", "POST", "PUT", "DELETE")));

var app = builder.Build();

app.UseMiddleware<ProblemDetailsMiddleware>();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    await next();
});
if (!isDevelopment)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCors("dev-ui");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
if (isDevelopment)
{
    app.MapOpenApi().AllowAnonymous();
}
app.MapGet("/health", async (CardDemoDbContext db, CancellationToken ct) =>
{
    var healthy = await db.Database.CanConnectAsync(ct);
    return healthy
        ? Results.Ok(new { status = "Healthy" })
        : Results.Json(new { status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.Run();

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
            return ProblemDetailsMiddleware.WriteAsync(context, 403, ErrorCodes.Forbidden,
                "You do not have permission to maintain transaction types.");
        }
        return _default.HandleAsync(next, context, policy, result);
    }
}

public partial class Program;
