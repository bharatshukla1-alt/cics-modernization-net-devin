using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SignOnService.Tests;

/// <summary>API integration tests against the PostgreSQL usrsec table (ConnectionStrings__SignOn).</summary>
public class ApiTests(ApiTests.Factory factory) : IClassFixture<ApiTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("RateLimit:SignInPerMinute", "1000");
        }
    }

    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(HttpStatusCode Status, JsonElement Body)> SignIn(object body)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/sign-in", body);
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<string> Token(string user)
    {
        var (_, body) = await SignIn(new { userId = user, password = "password" });
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> Get(string path, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    [Fact]
    public async Task Admin_signs_in_case_insensitively()
    {
        var (status, body) = await SignIn(new { userId = "admin001", password = "password" });
        Assert.Equal(HttpStatusCode.OK, status);
        var user = body.GetProperty("user");
        Assert.Equal("ADMIN001", user.GetProperty("userId").GetString());
        Assert.Equal("MARGARET", user.GetProperty("firstName").GetString());
        Assert.Equal("A", user.GetProperty("userType").GetString());
    }

    [Theory]
    [InlineData("NOUSER01", "PASSWORD", HttpStatusCode.Unauthorized, "USER_NOT_FOUND")]
    [InlineData("USER0001", "WRONG", HttpStatusCode.Unauthorized, "WRONG_PASSWORD")]
    [InlineData("", "PASSWORD", HttpStatusCode.BadRequest, "USER_ID_REQUIRED")]
    [InlineData("USER0001", "", HttpStatusCode.BadRequest, "PASSWORD_REQUIRED")]
    public async Task Sign_in_errors_use_problem_codes(string id, string pwd, HttpStatusCode expected, string code)
    {
        var (status, body) = await SignIn(new { userId = id, password = pwd });
        Assert.Equal(expected, status);
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unknown_fields_and_malformed_json_are_rejected()
    {
        var (status, body) = await SignIn(new { userId = "USER0001", password = "PASSWORD", role = "A" });
        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("VALIDATION_FAILED", body.GetProperty("code").GetString());

        var raw = await _client.PostAsync("/api/v1/auth/sign-in", new StringContent("{", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, raw.StatusCode);
    }

    [Fact]
    public async Task Menus_require_matching_role()
    {
        var admin = await Token("ADMIN001");
        var user = await Token("USER0001");

        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/api/v1/menus/admin", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Get("/api/v1/menus/admin", user)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Get("/api/v1/menus/main", admin)).StatusCode);

        var adminMenu = await (await Get("/api/v1/menus/admin", admin)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(6, adminMenu.GetProperty("options").GetArrayLength());
        Assert.Equal("/transaction-types", adminMenu.GetProperty("options")[4].GetProperty("route").GetString());

        var mainMenu = await (await Get("/api/v1/menus/main", user)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(11, mainMenu.GetProperty("options").GetArrayLength());
        Assert.Equal("NOT_INSTALLED", mainMenu.GetProperty("options")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Discovery_and_jwks_are_published()
    {
        var discovery = await _client.GetFromJsonAsync<JsonElement>("/.well-known/openid-configuration");
        Assert.Equal("http://localhost:5001", discovery.GetProperty("issuer").GetString());
        Assert.EndsWith("/.well-known/jwks.json", discovery.GetProperty("jwks_uri").GetString());

        var jwks = await _client.GetFromJsonAsync<JsonElement>("/.well-known/jwks.json");
        var key = jwks.GetProperty("keys")[0];
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.False(key.TryGetProperty("d", out _));
    }

    [Fact]
    public async Task Health_and_security_headers()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
    }
}
