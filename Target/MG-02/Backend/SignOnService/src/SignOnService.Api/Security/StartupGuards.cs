namespace SignOnService.Api.Security;

/// <summary>Refuses unsafe configuration outside Development.</summary>
public static class StartupGuards
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configuration["Auth:Issuer"]))
            throw new InvalidOperationException("Auth:Issuer must be configured.");
        if (environment.IsDevelopment()) return;

        if (!configuration.GetValue<bool>("Auth:AllowLegacyPlainTextPasswords"))
            throw new InvalidOperationException(
                "Plain-text password verification is demo-only. Set Auth:AllowLegacyPlainTextPasswords=true to acknowledge.");
        if (string.IsNullOrWhiteSpace(configuration["Auth:SigningKeyPath"]))
            throw new InvalidOperationException("Auth:SigningKeyPath (RSA PEM) is required outside Development.");
        if (!Uri.TryCreate(configuration["Auth:Issuer"], UriKind.Absolute, out var issuer) || issuer.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Auth:Issuer must be an https URL outside Development.");
        var hosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(hosts) || hosts.Trim() == "*")
            throw new InvalidOperationException("AllowedHosts must list explicit host names outside Development.");
    }
}
