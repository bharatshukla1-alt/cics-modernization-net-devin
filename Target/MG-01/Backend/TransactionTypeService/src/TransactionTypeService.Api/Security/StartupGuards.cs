namespace TransactionTypeService.Api.Security;

/// <summary>Fails fast at startup when the security configuration is unsafe for the current environment.</summary>
public static class StartupGuards
{
    /// <summary>Throws <see cref="InvalidOperationException"/> when a mandated control is not configured.</summary>
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (configuration.GetValue<bool>("Auth:DevBypass") && !environment.IsDevelopment())
        {
            throw new InvalidOperationException("Auth:DevBypass is only allowed in the Development environment.");
        }
        if (environment.IsDevelopment())
        {
            return;
        }

        if (!Uri.TryCreate(configuration["Auth:Authority"], UriKind.Absolute, out var authority) || authority.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Auth:Authority must be an https URL outside Development.");
        }

        var hasHttpsEndpoint = configuration.GetSection("Kestrel:Endpoints").GetChildren()
            .Any(e => e["Url"]?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true);
        if (!hasHttpsEndpoint && !configuration.GetValue<bool>("Tls:TerminatedUpstream"))
        {
            throw new InvalidOperationException(
                "Configure a Kestrel HTTPS endpoint (TLS 1.3) or set Tls:TerminatedUpstream=true when TLS 1.3 is terminated at the gateway.");
        }

        var allowedHosts = configuration["AllowedHosts"]?.Trim();
        if (string.IsNullOrEmpty(allowedHosts) || allowedHosts == "*")
        {
            throw new InvalidOperationException("AllowedHosts must list the service host names outside Development.");
        }
    }
}
