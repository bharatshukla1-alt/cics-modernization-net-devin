namespace UserAdminService.Api.Security;

/// <summary>Refuses unsafe configuration outside Development.</summary>
public static class StartupGuards
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(configuration["Auth:Authority"]))
            throw new InvalidOperationException("Auth:Authority (SignOnService issuer URL) must be configured.");
        if (environment.IsDevelopment()) return;

        if (!configuration.GetValue<bool>("Auth:AllowLegacyPlainTextPasswords"))
            throw new InvalidOperationException(
                "Plain-text password storage is demo-only. Set Auth:AllowLegacyPlainTextPasswords=true to acknowledge.");
        if (!Uri.TryCreate(configuration["Auth:Authority"], UriKind.Absolute, out var authority) || authority.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Auth:Authority must be an https URL outside Development.");
        var hosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(hosts) || hosts.Trim() == "*")
            throw new InvalidOperationException("AllowedHosts must list explicit host names outside Development.");
    }
}
