using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace TransactionTypeService.Tests;

/// <summary>Runs against the Phase 5 PostgreSQL database (ConnectionStrings__CardDemo). Uses codes 90-99 and cleans up.</summary>
public class ApiIntegrationTests : IClassFixture<ApiIntegrationTests.Factory>, IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__CardDemo")
        ?? throw new InvalidOperationException("Set ConnectionStrings__CardDemo to run integration tests.");

    // Fixture setup needs DDL (dependent-table test); the API itself runs as the least-privilege login.
    private static readonly string AdminConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__CardDemoAdmin") ?? ConnectionString;

    private readonly HttpClient _client;
    private readonly HttpClient _anonymous;

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Auth:DevBypass", "true");
            builder.UseSetting("ConnectionStrings:CardDemo", ConnectionString);
        }
    }

    public ApiIntegrationTests(Factory factory)
    {
        _anonymous = factory.CreateClient();
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "dummy-dev-token");
    }

    public Task InitializeAsync() => ExecAsync("DELETE FROM transaction_type WHERE tr_type >= '90'");

    public async Task DisposeAsync()
    {
        await ExecAsync("DROP TABLE IF EXISTS mg01_test_child");
        await ExecAsync("DELETE FROM transaction_type WHERE tr_type >= '90'");
    }

    [Fact]
    public async Task List_returns_seed_in_code_order_seven_per_page()
    {
        var page = await GetJson("/api/v1/transaction-types");
        var codes = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("typeCode").GetString()).ToList();
        Assert.Equal(["01", "02", "03", "04", "05", "06", "07"], codes);
        Assert.False(page.GetProperty("hasNext").GetBoolean());
        Assert.False(page.GetProperty("hasPrevious").GetBoolean());
        Assert.False(page.GetProperty("filtered").GetBoolean());
    }

    [Fact]
    public async Task Keyset_paging_forward_and_backward()
    {
        for (var i = 90; i <= 92; i++)
        {
            await ExecAsync($"INSERT INTO transaction_type VALUES ('{i}', 'TEST {i}')");
        }
        var first = await GetJson("/api/v1/transaction-types");
        Assert.True(first.GetProperty("hasNext").GetBoolean());
        Assert.Equal("07", first.GetProperty("lastCode").GetString());

        var second = await GetJson("/api/v1/transaction-types?after=07");
        Assert.Equal("90", second.GetProperty("firstCode").GetString());
        Assert.Equal("92", second.GetProperty("lastCode").GetString());
        Assert.False(second.GetProperty("hasNext").GetBoolean());
        Assert.True(second.GetProperty("hasPrevious").GetBoolean());

        var back = await GetJson("/api/v1/transaction-types?before=90");
        Assert.Equal("01", back.GetProperty("firstCode").GetString());
        Assert.False(back.GetProperty("hasPrevious").GetBoolean());
        Assert.True(back.GetProperty("hasNext").GetBoolean());

        var beforeFirst = await GetJson("/api/v1/transaction-types?before=01");
        Assert.Equal("01", beforeFirst.GetProperty("firstCode").GetString());
    }

    [Theory]
    [InlineData("typeCode=5", new[] { "05" })]
    [InlineData("typeCode=00", new[] { "01", "02", "03", "04", "05", "06", "07" })]
    [InlineData("description=PAY", new[] { "02" })]
    [InlineData("description=pay", new string[0])]
    [InlineData("description=%25", new string[0])]
    public async Task Filters(string query, string[] expected)
    {
        var page = await GetJson($"/api/v1/transaction-types?{query}");
        var codes = page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("typeCode").GetString()!).ToArray();
        Assert.Equal(expected, codes);
    }

    [Fact]
    public async Task Invalid_type_filter_is_400()
    {
        var response = await _client.GetAsync("/api/v1/transaction-types?typeCode=AB");
        await AssertProblem(response, HttpStatusCode.BadRequest, "TYPE_FILTER_INVALID");
    }

    [Fact]
    public async Task Get_by_code_and_not_found()
    {
        var found = await GetJson("/api/v1/transaction-types/5");
        Assert.Equal("05", found.GetProperty("typeCode").GetString());
        var missing = await _client.GetAsync("/api/v1/transaction-types/12");
        var problem = await AssertProblem(missing, HttpStatusCode.NotFound, "TYPE_NOT_FOUND");
        Assert.Equal("No transaction type with code 12.", problem.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_update_delete_round_trip()
    {
        var created = await _client.PostAsJsonAsync("/api/v1/transaction-types", new { typeCode = "95", description = "  Fee reversal " });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/v1/transaction-types/95", created.Headers.Location!.ToString());
        Assert.Equal("Fee reversal", (await GetJson("/api/v1/transaction-types/95")).GetProperty("description").GetString());

        var duplicate = await _client.PostAsJsonAsync("/api/v1/transaction-types", new { typeCode = "95", description = "Again" });
        await AssertProblem(duplicate, HttpStatusCode.Conflict, "TYPE_ALREADY_EXISTS");

        var noChange = await _client.PutAsJsonAsync("/api/v1/transaction-types/95", new { description = "FEE REVERSAL" });
        await AssertProblem(noChange, HttpStatusCode.UnprocessableEntity, "NO_CHANGES");

        var updated = await _client.PutAsJsonAsync("/api/v1/transaction-types/95", new { description = "Fee reversal 2" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync("/api/v1/transaction-types/95")).StatusCode);
        await AssertProblem(await _client.DeleteAsync("/api/v1/transaction-types/95"), HttpStatusCode.NotFound, "TYPE_NOT_FOUND");
        await AssertProblem(await _client.PutAsJsonAsync("/api/v1/transaction-types/95", new { description = "X" }),
            HttpStatusCode.NotFound, "TYPE_NOT_FOUND");
    }

    [Fact]
    public async Task Single_digit_code_is_created_zero_padded()
    {
        await ExecAsync("DELETE FROM transaction_type WHERE tr_type = '09'");
        try
        {
            var created = await _client.PostAsJsonAsync("/api/v1/transaction-types", new { typeCode = "9", description = "Nine" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal("09", (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("typeCode").GetString());
        }
        finally
        {
            await ExecAsync("DELETE FROM transaction_type WHERE tr_type = '09'");
        }
    }

    [Theory]
    [InlineData("", "Fine", "typeCode", "Type code is required.")]
    [InlineData("AB", "Fine", "typeCode", "Type code must be numeric.")]
    [InlineData("00", "Fine", "typeCode", "Type code must not be zero.")]
    [InlineData("96", "", "description", "Description is required.")]
    [InlineData("96", "Bad-Desc", "description", "Description can contain only letters, numbers and spaces.")]
    public async Task Create_validation(string code, string description, string field, string message)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transaction-types", new { typeCode = code, description });
        var problem = await AssertProblem(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        Assert.Equal(message, problem.GetProperty("errors").GetProperty(field)[0].GetString());
    }

    [Fact]
    public async Task Unknown_json_member_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transaction-types", new { typeCode = "97", description = "X", extra = 1 });
        await AssertProblem(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    }

    [Fact]
    public async Task Delete_with_dependent_rows_is_409()
    {
        await ExecAsync("INSERT INTO transaction_type VALUES ('98', 'PARENT')");
        await ExecAsync("CREATE TABLE IF NOT EXISTS mg01_test_child (code CHAR(2) REFERENCES transaction_type(tr_type) ON DELETE RESTRICT)");
        await ExecAsync("INSERT INTO mg01_test_child VALUES ('98')");
        var problem = await AssertProblem(await _client.DeleteAsync("/api/v1/transaction-types/98"), HttpStatusCode.Conflict, "HAS_DEPENDENT_RECORDS");
        Assert.Equal("Please delete associated child records first.", problem.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Locked_row_update_is_409()
    {
        await ExecAsync("INSERT INTO transaction_type VALUES ('94', 'LOCKED')");
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await using (var cmd = new NpgsqlCommand("SELECT * FROM transaction_type WHERE tr_type = '94' FOR UPDATE", conn, tx))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        var response = await _client.PutAsJsonAsync("/api/v1/transaction-types/94", new { description = "Changed" });
        await AssertProblem(response, HttpStatusCode.Conflict, "RECORD_LOCKED");
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task Missing_token_is_401_and_health_is_anonymous()
    {
        await AssertProblem(await _anonymous.GetAsync("/api/v1/transaction-types"), HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        Assert.Equal(HttpStatusCode.OK, (await _anonymous.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Security_headers_are_set()
    {
        var response = await _client.GetAsync("/api/v1/transaction-types");
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    private async Task<JsonElement> GetJson(string url)
    {
        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
        return body;
    }

    private static async Task ExecAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(AdminConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}
