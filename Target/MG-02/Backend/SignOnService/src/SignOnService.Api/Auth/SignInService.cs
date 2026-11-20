using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using SignOnService.Api.Audit;
using SignOnService.Api.Data;
using SignOnService.Api.Errors;

namespace SignOnService.Api.Auth;

public sealed record SignInRequest(string? UserId, string? Password);

public sealed record UserInfo(string UserId, string FirstName, string LastName, string UserType);

public sealed record SignInResponse(string AccessToken, string TokenType, int ExpiresIn, UserInfo User);

/// <summary>COSGN00C PROCESS-ENTER-KEY + READ-USER-SEC-FILE.</summary>
public sealed class SignInService(ISecurityUserStore store, TokenIssuer issuer, IAuditLog audit, ILogger<SignInService> logger)
{
    public const int MaxLength = 8;
    public const string AuditAction = "auth.sign-in";

    public async Task<SignInResponse> SignInAsync(SignInRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ApiException(400, ErrorCodes.UserIdRequired, "Please enter User ID ...");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ApiException(400, ErrorCodes.PasswordRequired, "Please enter Password ...");

        var userId = request.UserId.TrimEnd().ToUpperInvariant();
        var password = request.Password.TrimEnd().ToUpperInvariant();
        if (userId.Length > MaxLength || password.Length > MaxLength)
            throw new ApiException(400, ErrorCodes.FieldTooLong, "User ID and password are at most 8 characters.");

        SecurityUser? user;
        try
        {
            user = await store.FindAsync(userId, ct);
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or InvalidOperationException)
        {
            logger.LogError(ex, "USRSEC lookup failed");
            audit.Write(AuditAction, userId, "failure", ErrorCodes.UnableToVerify);
            throw new ApiException(503, ErrorCodes.UnableToVerify, "Unable to verify the User ...");
        }

        if (user is null)
        {
            audit.Write(AuditAction, MaskUnknownUserId(userId), "failure", ErrorCodes.UserNotFound);
            throw new ApiException(401, ErrorCodes.UserNotFound, "User not found. Try again ...");
        }
        if (!PasswordMatches(password, user.Password))
        {
            audit.Write(AuditAction, userId, "failure", ErrorCodes.WrongPassword);
            throw new ApiException(401, ErrorCodes.WrongPassword, "Wrong Password. Try again ...");
        }

        var (token, expiresIn) = issuer.Issue(user);
        audit.Write(AuditAction, userId, "success");
        return new SignInResponse(token, "Bearer", expiresIn,
            new UserInfo(user.UserId, user.FirstName, user.LastName, user.UserType));
    }

    /// <summary>An unknown user ID may be a mistyped password, so audit only a hash prefix of it.</summary>
    public static string MaskUnknownUserId(string userId) =>
        "unknown#" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)))[..12].ToLowerInvariant();

    /// <summary>Legacy plain-text comparison (SEC-USR-PWD = WS-USER-PWD), in constant time. Demo only.</summary>
    public static bool PasswordMatches(string entered, string stored) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(entered), Encoding.UTF8.GetBytes(stored));
}
