using UserAdminService.Api.Errors;

namespace UserAdminService.Api.Users;

/// <summary>COUSR01C/COUSR02C field checks in legacy order, plus the fixed gaps G1 (type) and G2 (upper-case).</summary>
public static class UserValidator
{
    public const int IdLength = 8;
    public const int NameLength = 20;
    public const int PasswordLength = 8;

    public sealed record ValidUser(string UserId, string FirstName, string LastName, string? Password, string UserType);

    public static ValidUser ValidateCreate(CreateUserRequest r)
    {
        var first = Required(r.FirstName, ErrorCodes.FirstNameRequired, "First Name can NOT be empty...");
        var last = Required(r.LastName, ErrorCodes.LastNameRequired, "Last Name can NOT be empty...");
        var id = NormalizeUserId(Required(r.UserId, ErrorCodes.UserIdRequired, "User ID can NOT be empty..."));
        var password = Required(r.Password, ErrorCodes.PasswordRequired, "Password can NOT be empty...");
        var type = Required(r.UserType, ErrorCodes.UserTypeRequired, "User Type can NOT be empty...");
        return new(id, Max(first, NameLength, "First Name"), Max(last, NameLength, "Last Name"),
            NormalizePassword(password), NormalizeType(type));
    }

    public static ValidUser ValidateUpdate(string userId, UpdateUserRequest r)
    {
        var id = NormalizeUserId(Required(userId, ErrorCodes.UserIdRequired, "User ID can NOT be empty..."));
        var first = Required(r.FirstName, ErrorCodes.FirstNameRequired, "First Name can NOT be empty...");
        var last = Required(r.LastName, ErrorCodes.LastNameRequired, "Last Name can NOT be empty...");
        var type = Required(r.UserType, ErrorCodes.UserTypeRequired, "User Type can NOT be empty...");
        var password = string.IsNullOrWhiteSpace(r.Password) ? null : NormalizePassword(r.Password);
        return new(id, Max(first, NameLength, "First Name"), Max(last, NameLength, "Last Name"), password, NormalizeType(type));
    }

    public static string NormalizeUserId(string value)
    {
        var id = Max(value.Trim(), IdLength, "User ID").ToUpperInvariant();
        if (!id.All(char.IsAsciiLetterOrDigit))
            throw Bad(ErrorCodes.InvalidUserId, "User ID must contain only letters and digits...");
        return id;
    }

    private static string NormalizePassword(string value) => Max(value, PasswordLength, "Password").ToUpperInvariant();

    private static string NormalizeType(string value)
    {
        var type = value.Trim().ToUpperInvariant();
        if (type is not ("A" or "U"))
            throw Bad(ErrorCodes.InvalidUserType, "User Type must be A (Admin) or U (User)...");
        return type;
    }

    private static string Required(string? value, string code, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw Bad(code, message) : value.TrimEnd();

    private static string Max(string value, int max, string field) =>
        value.Length > max ? throw Bad(ErrorCodes.FieldTooLong, $"{field} must be {max} characters or fewer...") : value;

    private static ApiException Bad(string code, string message) => new(StatusCodes.Status400BadRequest, code, message);
}
