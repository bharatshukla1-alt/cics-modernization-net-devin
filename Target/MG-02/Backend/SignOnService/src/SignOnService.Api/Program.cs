using System.Net;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SignOnService.Api.Audit;
using SignOnService.Api.Auth;
using SignOnService.Api.Data;
using SignOnService.Api.Errors;
using SignOnService.Api.Endpoints;
using SignOnService.Api.Menus;
using SignOnService.Api.Security;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
StartupGuards.Validate(builder.Configuration, builder.Environment);

var connectionString = builder.Configuration.GetConnectionString("SignOn")
                       ?? throw new InvalidOperationException("Connection string 'SignOn' is not configured (set ConnectionStrings__SignOn).");
var authOptions = builder.Configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();
var menuOptions = new MenuRouteOptions();
builder.Configuration.GetSection("Menu").Bind(menuOptions);
var signingKeys = new SigningKeys(authOptions);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024;
    options.AddServerHeader = false;
    if (!isDevelopment)
    {
        options.ConfigureHttpsDefaults(https => https.SslProtocols = SslProtocols.Tls13);
    }
});

builder.Services.AddDbContext<SignOnDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<ISecurityUserStore, SecurityUserStore>();
builder.Services.AddSingleton(authOptions);
builder.Services.AddSingleton(menuOptions);
builder.Services.AddSingleton(signingKeys);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<TokenIssuer>();
builder.Services.AddScoped<SignInService>();
builder.Services.AddSingleton<MenuService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditLog, AuditLog>();
builder.Services.AddOpenApi();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters.ValidIssuer = authOptions.Issuer;
        o.TokenValidationParameters.ValidAudience = authOptions.Audience;
        o.TokenValidationParameters.IssuerSigningKey = signingKeys.Key;
        o.TokenValidationParameters.NameClaimType = "sub";
        o.TokenValidationParameters.RoleClaimType = "role";
        o.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
    });
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Roles.User, p => p.RequireAuthenticatedUser().RequireRole(Roles.User));
    o.AddPolicy(Roles.Admin, p => p.RequireAuthenticatedUser().RequireRole(Roles.Admin));
});
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemAuthorizationResultHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    o.ForwardLimit = 1;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        o.KnownProxies.Add(IPAddress.Parse(proxy));
    }
});

var signInPerMinute = builder.Configuration.GetValue("RateLimit:SignInPerMinute", 5);
builder.Services.AddRateLimiter(o =>
{
    o.OnRejected = (ctx, _) => new ValueTask(ProblemDetailsMiddleware.WriteAsync(ctx.HttpContext, 429,
        ErrorCodes.TooManyRequests, "Too many sign-in attempts. Wait a minute and try again."));
    o.AddPolicy("sign-in", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = signInPerMinute, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddCors(o => o.AddPolicy("dev-ui", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .WithMethods("GET", "POST")));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ProblemDetailsMiddleware>();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Cache-Control"] = "no-store";
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

app.MapAuthEndpoints();
app.MapMenuEndpoints();
app.MapWellKnownEndpoints(authOptions);
if (isDevelopment)
{
    app.MapOpenApi().AllowAnonymous();
}
app.MapHealthEndpoint();

app.Run();

public partial class Program;
