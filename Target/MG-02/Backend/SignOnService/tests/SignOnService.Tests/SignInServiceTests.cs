using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SignOnService.Api.Audit;
using SignOnService.Api.Auth;
using SignOnService.Api.Data;
using SignOnService.Api.Errors;

namespace SignOnService.Tests;

public class SignInServiceTests
{
    private sealed class FakeStore(Func<string, SecurityUser?> find) : ISecurityUserStore
    {
        public List<string> Lookups { get; } = [];
        public Task<SecurityUser?> FindAsync(string userId, CancellationToken ct)
        {
            Lookups.Add(userId);
            return Task.FromResult(find(userId));
        }
    }

    private sealed class FakeAudit : IAuditLog
    {
        public List<(string Action, string User, string Outcome, string? Reason)> Events { get; } = [];
        public void Write(string action, string user, string outcome, string? reason = null) => Events.Add((action, user, outcome, reason));
    }

    private static readonly AuthOptions Options = new() { Issuer = "http://test", Audience = "carddemo-api", TokenMinutes = 15 };
    private static readonly SigningKeys Keys = new(Options);

    private static SecurityUser User(string id, string type) =>
        new() { UserId = id, FirstName = "Ann", LastName = "Lee", Password = "PASSWORD", UserType = type };

    private static (SignInService Service, FakeStore Store, FakeAudit Audit) Create(Func<string, SecurityUser?> find)
    {
        var store = new FakeStore(find);
        var audit = new FakeAudit();
        var issuer = new TokenIssuer(Keys, Options, TimeProvider.System);
        return (new SignInService(store, issuer, audit, NullLogger<SignInService>.Instance), store, audit);
    }

    private static async Task<ApiException> Fails(SignInService service, string? id, string? pwd) =>
        await Assert.ThrowsAsync<ApiException>(() => service.SignInAsync(new SignInRequest(id, pwd), CancellationToken.None));

    [Theory]
    [InlineData(null, "x")]
    [InlineData("", "x")]
    [InlineData("   ", null)]
    public async Task User_id_is_required_first(string? id, string? pwd)
    {
        var ex = await Fails(Create(_ => null).Service, id, pwd);
        Assert.Equal((400, ErrorCodes.UserIdRequired, "Please enter User ID ..."), (ex.Status, ex.Code, ex.Message));
    }

    [Fact]
    public async Task Password_is_required()
    {
        var ex = await Fails(Create(_ => null).Service, "USER0001", " ");
        Assert.Equal((400, ErrorCodes.PasswordRequired, "Please enter Password ..."), (ex.Status, ex.Code, ex.Message));
    }

    [Theory]
    [InlineData("USER00011", "PASSWORD")]
    [InlineData("USER0001", "PASSWORD1")]
    public async Task Values_longer_than_eight_are_rejected(string id, string pwd)
    {
        var ex = await Fails(Create(_ => null).Service, id, pwd);
        Assert.Equal(ErrorCodes.FieldTooLong, ex.Code);
    }

    [Fact]
    public async Task Unknown_user_is_not_found_and_audited()
    {
        var (service, _, audit) = Create(_ => null);
        var ex = await Fails(service, "nouser01", "x");
        Assert.Equal((401, ErrorCodes.UserNotFound, "User not found. Try again ..."), (ex.Status, ex.Code, ex.Message));
        Assert.Equal(("auth.sign-in", SignInService.MaskUnknownUserId("NOUSER01"), "failure", ErrorCodes.UserNotFound), audit.Events.Single());
        Assert.DoesNotContain("NOUSER01", audit.Events.Single().User);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var ex = await Fails(Create(_ => User("USER0001", "U")).Service, "USER0001", "WRONG");
        Assert.Equal((401, ErrorCodes.WrongPassword, "Wrong Password. Try again ..."), (ex.Status, ex.Code, ex.Message));
    }

    [Fact]
    public async Task Store_failure_is_unable_to_verify()
    {
        var ex = await Fails(Create(_ => throw new TimeoutException()).Service, "USER0001", "PASSWORD");
        Assert.Equal((503, ErrorCodes.UnableToVerify, "Unable to verify the User ..."), (ex.Status, ex.Code, ex.Message));
    }

    [Fact]
    public async Task Credentials_are_upper_cased_before_lookup_and_compare()
    {
        var (service, store, audit) = Create(id => id == "USER0001" ? User("USER0001", "U") : null);
        var result = await service.SignInAsync(new SignInRequest("user0001", "PassWord"), CancellationToken.None);
        Assert.Equal("USER0001", store.Lookups.Single());
        Assert.Equal("U", result.User.UserType);
        Assert.Equal("success", audit.Events.Single().Outcome);
    }

    [Fact]
    public async Task Admin_token_carries_admin_role_and_scopes()
    {
        var result = await Create(_ => User("ADMIN001", "A")).Service.SignInAsync(new SignInRequest("ADMIN001", "PASSWORD"), CancellationToken.None);
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(result.AccessToken, new TokenValidationParameters
        {
            ValidIssuer = "http://test", ValidAudience = "carddemo-api", IssuerSigningKey = Keys.Key
        });
        Assert.True(validation.IsValid);
        Assert.Equal("ADMIN001", validation.Claims["sub"]);
        Assert.Equal(Roles.Admin, validation.Claims["role"]);
        Assert.Equal(Roles.AdminScopes, validation.Claims["scope"]);
        Assert.Equal(("Bearer", 900), (result.TokenType, result.ExpiresIn));
    }

    [Fact]
    public async Task Non_admin_type_gets_user_role_without_scopes()
    {
        var result = await Create(_ => User("USER0001", "U")).Service.SignInAsync(new SignInRequest("USER0001", "PASSWORD"), CancellationToken.None);
        var token = new JsonWebToken(result.AccessToken);
        Assert.Equal(Roles.User, token.GetClaim("role").Value);
        Assert.False(token.TryGetClaim("scope", out _));
    }

    [Fact]
    public async Task Unexpected_errors_are_not_reported_as_unable_to_verify()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Create(_ => throw new ArgumentException("bug")).Service.SignInAsync(new SignInRequest("USER0001", "PASSWORD"), CancellationToken.None));
    }

    [Fact]
    public async Task Token_does_not_carry_personal_names()
    {
        var result = await Create(_ => User("USER0001", "U")).Service.SignInAsync(new SignInRequest("USER0001", "PASSWORD"), CancellationToken.None);
        Assert.False(new JsonWebToken(result.AccessToken).TryGetClaim("name", out _));
    }

    [Fact]
    public void Password_comparison_is_exact()
    {
        Assert.True(SignInService.PasswordMatches("PASSWORD", "PASSWORD"));
        Assert.False(SignInService.PasswordMatches("PASSWORD", "password"));
        Assert.False(SignInService.PasswordMatches("PASS", "PASSWORD"));
    }
}
