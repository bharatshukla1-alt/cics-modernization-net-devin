using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace SignOnService.Tests;

/// <summary>SEC-002: the sign-in limit applies per real client behind a trusted proxy, not per proxy.</summary>
public class RateLimitTests(RateLimitTests.Factory factory) : IClassFixture<RateLimitTests.Factory>
{
    private sealed class ProxyConnectionFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse("10.9.9.9");
                return nextMiddleware(context);
            });
            next(app);
        };
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("RateLimit:SignInPerMinute", "2");
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.9.9.9");
            builder.ConfigureServices(s => s.AddSingleton<IStartupFilter, ProxyConnectionFilter>());
        }
    }

    private async Task<HttpStatusCode> SignInFrom(string clientIp)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/sign-in")
        {
            Content = JsonContent.Create(new { userId = "", password = "" })
        };
        request.Headers.Add("X-Forwarded-For", clientIp);
        return (await factory.CreateClient().SendAsync(request)).StatusCode;
    }

    [Fact]
    public async Task Clients_behind_the_same_proxy_have_separate_limits()
    {
        Assert.Equal(HttpStatusCode.BadRequest, await SignInFrom("203.0.113.1"));
        Assert.Equal(HttpStatusCode.BadRequest, await SignInFrom("203.0.113.1"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await SignInFrom("203.0.113.1"));
        Assert.Equal(HttpStatusCode.BadRequest, await SignInFrom("203.0.113.2"));
    }
}
