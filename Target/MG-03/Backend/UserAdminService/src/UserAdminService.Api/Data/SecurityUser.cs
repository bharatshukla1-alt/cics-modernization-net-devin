namespace UserAdminService.Api.Data;

/// <summary>USRSEC record (CSUSR01Y). <see cref="Version"/> is PostgreSQL <c>xmin</c>.</summary>
public sealed class SecurityUser
{
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Password { get; set; } = "";
    public string UserType { get; set; } = "";
    public uint Version { get; set; }
}
