using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace UserAdminService.Tests;

/// <summary>API integration tests against PostgreSQL usrsec (ConnectionStrings__UserAdmin). Test users use the ZZ prefix.</summary>
public class ApiTests(ApiTests.Factory factory) : IClassFixture<ApiTests.Factory>, IAsyncLifetime
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        public static readonly RsaSecurityKey Key = new(RSA.Create(2048)) { KeyId = "test" };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(s => s.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.Authority = null;
                o.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = "test-issuer" };
                o.TokenValidationParameters.ValidIssuer = "test-issuer";
                o.TokenValidationParameters.IssuerSigningKey = Key;
            }));
        }
    }

    private readonly HttpClient _client = factory.CreateClient();

    private static string Token(string sub, string role, string? scope)
    {
        var claims = new Dictionary<string, object> { ["sub"] = sub, ["role"] = role };
        if (scope is not null) claims["scope"] = scope;
        return new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = "test-issuer", Audience = "carddemo-api", Claims = claims, Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(Factory.Key, SecurityAlgorithms.RsaSha256),
        });
    }

    private static readonly string Admin = Token("ADMIN001", "carddemo.admin", "users.read users.write");

    private async Task<(HttpStatusCode Status, JsonElement Body, HttpResponseMessage Response)> Send(
        HttpMethod method, string path, object? body = null, string? token = null, string? ifMatch = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? Admin);
        if (token == "") request.Headers.Authorization = null;
        if (ifMatch is not null) request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await _client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, text.Length > 0 ? JsonDocument.Parse(text).RootElement.Clone() : default, response);
    }

    private static string Code(JsonElement body) => body.GetProperty("code").GetString()!;

    private async Task Cleanup()
    {
        foreach (var id in new[] { "ZZTEST01", "ZZTEST02", "ZZADMIN1" })
        {
            var (status, body, _) = await Send(HttpMethod.Get, $"/api/v1/users/{id}");
            if (status == HttpStatusCode.OK)
                await Send(HttpMethod.Delete, $"/api/v1/users/{id}", ifMatch: $"\"{body.GetProperty("version").GetUInt32()}\"");
        }
    }

    public Task InitializeAsync() => Cleanup();
    public Task DisposeAsync() => Cleanup();

    [Fact]
    public async Task List_first_page_is_ten_users_in_key_order()
    {
        var (status, body, _) = await Send(HttpMethod.Get, "/api/v1/users");
        Assert.Equal(HttpStatusCode.OK, status);
        var ids = body.GetProperty("users").EnumerateArray().Select(u => u.GetProperty("userId").GetString()!).ToList();
        Assert.Equal(10, ids.Count);
        Assert.Equal(ids.OrderBy(i => i, StringComparer.Ordinal), ids);
        Assert.Equal("ADMIN001", ids[0]);
        Assert.False(body.GetProperty("hasPrevious").GetBoolean());
    }

    [Fact]
    public async Task Search_positions_at_user_id_and_paging_works()
    {
        var (_, body, _) = await Send(HttpMethod.Get, "/api/v1/users?search=user0003");
        Assert.Equal("USER0003", body.GetProperty("users")[0].GetProperty("userId").GetString());
        Assert.True(body.GetProperty("hasPrevious").GetBoolean());
        var (_, prev, _) = await Send(HttpMethod.Get, "/api/v1/users?before=USER0003");
        Assert.Equal("USER0002", prev.GetProperty("lastUserId").GetString());
        var (_, top, _) = await Send(HttpMethod.Get, "/api/v1/users?before=ADMIN001");
        Assert.Equal("You are already at the top of the page...", top.GetProperty("message").GetString());
        var (_, bottom, _) = await Send(HttpMethod.Get, "/api/v1/users?after=ZZZZZZZZ");
        Assert.Equal("You are already at the bottom of the page...", bottom.GetProperty("message").GetString());
        var (bad, badBody, _) = await Send(HttpMethod.Get, "/api/v1/users?search=A&after=B");
        Assert.Equal((HttpStatusCode.BadRequest, "INVALID_QUERY"), (bad, Code(badBody)));
    }

    [Fact]
    public async Task Create_update_delete_round_trip_with_legacy_messages()
    {
        var (created, body, response) = await Send(HttpMethod.Post, "/api/v1/users",
            new { userId = "zztest01", firstName = "Ann", lastName = "Lee", password = "secret", userType = "u" });
        Assert.Equal(HttpStatusCode.Created, created);
        Assert.Equal("User ZZTEST01 has been added ...", body.GetProperty("message").GetString());
        Assert.Equal("/api/v1/users/ZZTEST01", response.Headers.Location!.ToString());

        var (dup, dupBody, _) = await Send(HttpMethod.Post, "/api/v1/users",
            new { userId = "ZZTEST01", firstName = "A", lastName = "B", password = "x", userType = "U" });
        Assert.Equal((HttpStatusCode.Conflict, "User ID already exist..."), (dup, dupBody.GetProperty("message").GetString()));

        var (_, fetched, fetchResponse) = await Send(HttpMethod.Get, "/api/v1/users/zztest01");
        Assert.False(fetched.TryGetProperty("password", out _));
        Assert.Equal("U", fetched.GetProperty("userType").GetString());
        var etag = fetchResponse.Headers.ETag!.Tag;

        var (same, sameBody, _) = await Send(HttpMethod.Put, "/api/v1/users/ZZTEST01",
            new { firstName = "Ann", lastName = "Lee", password = "SECRET", userType = "U" }, ifMatch: etag);
        Assert.Equal((HttpStatusCode.BadRequest, "Please modify to update ..."), (same, sameBody.GetProperty("message").GetString()));

        var (noVersion, nvBody, _) = await Send(HttpMethod.Put, "/api/v1/users/ZZTEST01", new { firstName = "Ann", lastName = "Wong", userType = "U" });
        Assert.Equal(((HttpStatusCode)428, "VERSION_REQUIRED"), (noVersion, Code(nvBody)));

        var (updated, updBody, _) = await Send(HttpMethod.Put, "/api/v1/users/ZZTEST01",
            new { firstName = "Ann", lastName = "Wong", password = "", userType = "U" }, ifMatch: etag);
        Assert.Equal(HttpStatusCode.OK, updated);
        Assert.Equal("User ZZTEST01 has been updated ...", updBody.GetProperty("message").GetString());
        Assert.Equal("Wong", updBody.GetProperty("user").GetProperty("lastName").GetString());

        var (stale, staleBody, _) = await Send(HttpMethod.Delete, "/api/v1/users/ZZTEST01", ifMatch: etag);
        Assert.Equal((HttpStatusCode.PreconditionFailed, "STALE_USER"), (stale, Code(staleBody)));

        var newTag = $"\"{updBody.GetProperty("user").GetProperty("version").GetUInt32()}\"";
        var (deleted, delBody, _) = await Send(HttpMethod.Delete, "/api/v1/users/ZZTEST01", ifMatch: newTag);
        Assert.Equal(HttpStatusCode.OK, deleted);
        Assert.Equal("User ZZTEST01 has been deleted ...", delBody.GetProperty("message").GetString());

        var (gone, goneBody, _) = await Send(HttpMethod.Get, "/api/v1/users/ZZTEST01");
        Assert.Equal((HttpStatusCode.NotFound, "User ID NOT found..."), (gone, goneBody.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Create_validation_uses_legacy_order_and_new_type_rule()
    {
        var (s1, b1, _) = await Send(HttpMethod.Post, "/api/v1/users", new { userId = "", firstName = "", lastName = "", password = "", userType = "" });
        Assert.Equal((HttpStatusCode.BadRequest, "First Name can NOT be empty..."), (s1, b1.GetProperty("message").GetString()));
        var (_, b2, _) = await Send(HttpMethod.Post, "/api/v1/users", new { userId = "ZZTEST02", firstName = "A", lastName = "B", password = "P", userType = "X" });
        Assert.Equal("INVALID_USER_TYPE", Code(b2));
        var (s3, b3, _) = await Send(HttpMethod.Post, "/api/v1/users", new { userId = "ZZTEST02", firstName = "A", lastName = "B", password = "P", userType = "U", extra = 1 });
        Assert.Equal((HttpStatusCode.BadRequest, "VALIDATION_FAILED"), (s3, Code(b3)));
    }

    [Fact]
    public async Task Admin_cannot_delete_or_demote_self()
    {
        var (_, me, meResponse) = await Send(HttpMethod.Get, "/api/v1/users/ADMIN001");
        var tag = meResponse.Headers.ETag!.Tag;
        var (del, delBody, _) = await Send(HttpMethod.Delete, "/api/v1/users/ADMIN001", ifMatch: tag);
        Assert.Equal((HttpStatusCode.Conflict, "CANNOT_DELETE_SELF"), (del, Code(delBody)));
        var (dem, demBody, _) = await Send(HttpMethod.Put, "/api/v1/users/ADMIN001",
            new { firstName = me.GetProperty("firstName").GetString(), lastName = me.GetProperty("lastName").GetString(), userType = "U" }, ifMatch: tag);
        Assert.Equal((HttpStatusCode.Conflict, "CANNOT_DEMOTE_SELF"), (dem, Code(demBody)));
    }

    [Fact]
    public async Task Another_admin_can_be_demoted_and_deleted_while_admins_remain()
    {
        await Send(HttpMethod.Post, "/api/v1/users", new { userId = "ZZADMIN1", firstName = "Temp", lastName = "Admin", password = "P", userType = "A" });
        var (_, a, r) = await Send(HttpMethod.Get, "/api/v1/users/ZZADMIN1");
        var (st, body, _) = await Send(HttpMethod.Put, "/api/v1/users/ZZADMIN1", new { firstName = "Temp", lastName = "Admin", userType = "U" }, ifMatch: r.Headers.ETag!.Tag);
        Assert.Equal(HttpStatusCode.OK, st);
        Assert.Equal("U", body.GetProperty("user").GetProperty("userType").GetString());
    }

    [Theory]
    [InlineData("", "", 401)]
    [InlineData("carddemo.user", null, 403)]
    [InlineData("carddemo.admin", "transaction-types.read", 403)]
    public async Task Requires_admin_role_and_users_scope(string role, string? scope, int expected)
    {
        var token = role == "" ? "" : Token("USER0001", role, scope);
        var (status, _, _) = await Send(HttpMethod.Get, "/api/v1/users", token: token);
        Assert.Equal(expected, (int)status);
    }

    [Fact]
    public async Task Read_scope_cannot_write()
    {
        var token = Token("ADMIN001", "carddemo.admin", "users.read");
        var (status, _, _) = await Send(HttpMethod.Post, "/api/v1/users",
            new { userId = "ZZTEST02", firstName = "A", lastName = "B", password = "P", userType = "U" }, token: token);
        Assert.Equal(HttpStatusCode.Forbidden, status);
    }

    [Fact]
    public async Task Health_is_anonymous()
    {
        var (status, _, _) = await Send(HttpMethod.Get, "/health", token: "");
        Assert.Equal(HttpStatusCode.OK, status);
    }
}
