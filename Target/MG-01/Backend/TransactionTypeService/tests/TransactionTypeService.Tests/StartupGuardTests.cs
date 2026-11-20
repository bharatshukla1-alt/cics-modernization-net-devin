using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TransactionTypeService.Api.Security;

namespace TransactionTypeService.Tests;

public class StartupGuardTests
{
    private static readonly Dictionary<string, string?> SafeProduction = new()
    {
        ["Auth:Authority"] = "https://idp.example.test/realms/carddemo",
        ["Kestrel:Endpoints:Https:Url"] = "https://0.0.0.0:5001",
        ["AllowedHosts"] = "carddemo.example.test"
    };

    private static void Validate(Dictionary<string, string?> settings, string environment) =>
        StartupGuards.Validate(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new Env(environment));

    [Fact]
    public void Safe_production_configuration_passes() => Validate(SafeProduction, Environments.Production);

    [Theory]
    [InlineData("Auth:DevBypass", "true")]
    [InlineData("Auth:Authority", "http://idp.example.test")]
    [InlineData("Kestrel:Endpoints:Https:Url", "http://0.0.0.0:5000")]
    [InlineData("AllowedHosts", "*")]
    public void Unsafe_production_setting_fails(string key, string value)
    {
        var settings = new Dictionary<string, string?>(SafeProduction) { [key] = value };
        Assert.Throws<InvalidOperationException>(() => Validate(settings, Environments.Production));
    }

    [Fact]
    public void Upstream_tls_termination_is_accepted_when_declared()
    {
        var settings = new Dictionary<string, string?>(SafeProduction)
        {
            ["Kestrel:Endpoints:Https:Url"] = null,
            ["Tls:TerminatedUpstream"] = "true"
        };
        Validate(settings, Environments.Production);
    }

    [Fact]
    public void Development_allows_local_http_settings() =>
        Validate(new Dictionary<string, string?> { ["Auth:DevBypass"] = "true", ["Auth:Authority"] = "http://localhost:8081" }, Environments.Development);

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
