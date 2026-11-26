using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using UserAdminService.Api.Security;

namespace UserAdminService.Tests;

public class StartupGuardsTests
{
    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration Config(params (string, string)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Item1, v.Item2))).Build();

    [Fact]
    public void Authority_is_required() =>
        Assert.Throws<InvalidOperationException>(() => StartupGuards.Validate(Config(), new Env("Development")));

    [Fact]
    public void Production_requires_acknowledgement_https_and_hosts()
    {
        var prod = new Env("Production");
        Assert.Throws<InvalidOperationException>(() => StartupGuards.Validate(Config(("Auth:Authority", "https://idp")), prod));
        Assert.Throws<InvalidOperationException>(() => StartupGuards.Validate(
            Config(("Auth:Authority", "http://idp"), ("Auth:AllowLegacyPlainTextPasswords", "true"), ("AllowedHosts", "x")), prod));
        Assert.Throws<InvalidOperationException>(() => StartupGuards.Validate(
            Config(("Auth:Authority", "https://idp"), ("Auth:AllowLegacyPlainTextPasswords", "true"), ("AllowedHosts", "*")), prod));
        StartupGuards.Validate(Config(("Auth:Authority", "https://idp"), ("Auth:AllowLegacyPlainTextPasswords", "true"), ("AllowedHosts", "api")), prod);
    }
}
