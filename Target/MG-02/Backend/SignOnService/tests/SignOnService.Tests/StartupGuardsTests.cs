using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SignOnService.Api.Security;

namespace SignOnService.Tests;

public class StartupGuardsTests
{
    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value)).Build();

    private static readonly (string, string)[] Safe =
    [
        ("Auth:Issuer", "https://signon.example"), ("Auth:SigningKeyPath", "/keys/signon.pem"),
        ("Auth:AllowLegacyPlainTextPasswords", "true"), ("AllowedHosts", "signon.example")
    ];

    [Fact]
    public void Production_with_safe_settings_starts() => StartupGuards.Validate(Config(Safe), new Env("Production"));

    [Fact]
    public void Development_needs_only_an_issuer() =>
        StartupGuards.Validate(Config(("Auth:Issuer", "http://localhost:5001")), new Env("Development"));

    [Theory]
    [InlineData("Auth:Issuer", "")]
    [InlineData("Auth:Issuer", "http://signon.example")]
    [InlineData("Auth:SigningKeyPath", "")]
    [InlineData("Auth:AllowLegacyPlainTextPasswords", "false")]
    [InlineData("AllowedHosts", "*")]
    public void Production_rejects_unsafe_settings(string key, string value)
    {
        var values = Safe.Where(v => v.Item1 != key).Append((key, value)).ToArray();
        Assert.Throws<InvalidOperationException>(() => StartupGuards.Validate(Config(values), new Env("Production")));
    }
}
