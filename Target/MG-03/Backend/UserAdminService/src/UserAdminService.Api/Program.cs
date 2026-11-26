using System.Net;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using UserAdminService.Api.Audit;
using UserAdminService.Api.Data;
using UserAdminService.Api.Endpoints;
using UserAdminService.Api.Errors;
using UserAdminService.Api.Security;
using UserAdminService.Api.Users;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();
StartupGuards.Validate(builder.Configuration, builder.Environment);

var connectionString = builder.Configuration.GetConnectionString("UserAdmin")
                       ?? throw new InvalidOperationException("Connection string 'UserAdmin' is not configured (set ConnectionStrings__UserAdmin).");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2048;
    options.AddServerHeader = false;
    if (!isDevelopment)
    {
        options.ConfigureHttpsDefaults(https => https.SslProtocols = SslProtocols.Tls13);
    }
});

builder.Services.AddDbContext<UserAdminDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<UserService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditLog, AuditLog>();
builder.Services.AddOpenApi();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.UsersRead, p => p.RequireAuthenticatedUser().RequireRole(Policies.AdminRole)
        .RequireAssertion(c => Policies.HasScope(c.User, Policies.UsersRead)));
    o.AddPolicy(Policies.UsersWrite, p => p.RequireAuthenticatedUser().RequireRole(Policies.AdminRole)
        .RequireAssertion(c => Policies.HasScope(c.User, Policies.UsersWrite)));
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

builder.Services.AddCors(o => o.AddPolicy("dev-ui", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .WithExposedHeaders("ETag")
    .WithMethods("GET", "POST", "PUT", "DELETE")));

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
app.UseAuthorization();

app.MapUserEndpoints();
if (isDevelopment)
{
    app.MapOpenApi().AllowAnonymous();
}
app.MapHealthEndpoint();

app.Run();

public partial class Program;
