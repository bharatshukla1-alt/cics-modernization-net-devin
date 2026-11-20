using UserAdminService.Api.Errors;
using UserAdminService.Api.Users;

namespace UserAdminService.Tests;

public class UserValidatorTests
{
    private static string Code(Action act) => Assert.Throws<ApiException>(act).Code;

    [Theory]
    [InlineData(null, "L", "ID1", "PW", "U", ErrorCodes.FirstNameRequired)]
    [InlineData("F", " ", "ID1", "PW", "U", ErrorCodes.LastNameRequired)]
    [InlineData("F", "L", "", "PW", "U", ErrorCodes.UserIdRequired)]
    [InlineData("F", "L", "ID1", "", "U", ErrorCodes.PasswordRequired)]
    [InlineData("F", "L", "ID1", "PW", "", ErrorCodes.UserTypeRequired)]
    [InlineData("", "", "", "", "", ErrorCodes.FirstNameRequired)]
    [InlineData("F", "L", "ID1", "PW", "X", ErrorCodes.InvalidUserType)]
    [InlineData("F", "L", "ID-1", "PW", "U", ErrorCodes.InvalidUserId)]
    [InlineData("F", "L", "USER00010", "PW", "U", ErrorCodes.FieldTooLong)]
    [InlineData("F", "L", "ID1", "PASSWORD9", "U", ErrorCodes.FieldTooLong)]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU", "L", "ID1", "PW", "U", ErrorCodes.FieldTooLong)]
    public void Create_checks_fields_in_legacy_order(string? f, string? l, string? id, string? pw, string? type, string expected) =>
        Assert.Equal(expected, Code(() => UserValidator.ValidateCreate(new(id, f, l, pw, type))));

    [Fact]
    public void Create_upper_cases_id_password_and_type()
    {
        var v = UserValidator.ValidateCreate(new(" user0009", "Ann", "Lee", "secret", "a"));
        Assert.Equal(("USER0009", "SECRET", "A", "Ann"), (v.UserId, v.Password, v.UserType, v.FirstName));
    }

    [Fact]
    public void Update_blank_password_keeps_current()
    {
        var v = UserValidator.ValidateUpdate("user0001", new("F", "L", "  ", "u"));
        Assert.Null(v.Password);
        Assert.Equal("USER0001", v.UserId);
    }

    [Fact]
    public void Update_requires_names_and_type() =>
        Assert.Equal(ErrorCodes.UserTypeRequired, Code(() => UserValidator.ValidateUpdate("U1", new("F", "L", null, null))));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void Admin_guard_keeps_one_admin(int admins, bool allowed) => Assert.Equal(allowed, AdminGuard.CanRemoveAdmin(admins));
}
