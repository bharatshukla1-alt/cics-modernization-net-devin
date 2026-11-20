using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace SignOnService.Api.Auth;

public sealed class AuthOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "carddemo-api";
    public int TokenMinutes { get; set; } = 15;
    public string? SigningKeyPath { get; set; }
    public bool AllowLegacyPlainTextPasswords { get; set; }
}

/// <summary>RS256 signing key: PEM from <c>Auth:SigningKeyPath</c>, or an ephemeral key in Development.</summary>
public sealed class SigningKeys : IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);

    public SigningKeys(AuthOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SigningKeyPath))
        {
            _rsa.ImportFromPem(File.ReadAllText(options.SigningKeyPath));
        }
        var modulus = _rsa.ExportParameters(false).Modulus!;
        Key = new RsaSecurityKey(_rsa) { KeyId = Convert.ToHexString(SHA256.HashData(modulus))[..16].ToLowerInvariant() };
    }

    public RsaSecurityKey Key { get; }

    public object Jwks()
    {
        var p = _rsa.ExportParameters(false);
        return new
        {
            keys = new[]
            {
                new { kty = "RSA", use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = Key.KeyId,
                      n = Base64UrlEncoder.Encode(p.Modulus), e = Base64UrlEncoder.Encode(p.Exponent) }
            }
        };
    }

    public void Dispose() => _rsa.Dispose();
}
