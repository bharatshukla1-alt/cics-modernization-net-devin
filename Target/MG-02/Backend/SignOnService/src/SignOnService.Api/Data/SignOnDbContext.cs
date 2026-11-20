using Microsoft.EntityFrameworkCore;

namespace SignOnService.Api.Data;

/// <summary>USRSEC record (CSUSR01Y SEC-USER-DATA) as stored in table <c>usrsec</c>.</summary>
public sealed class SecurityUser
{
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Password { get; set; } = "";
    public string UserType { get; set; } = "";
}

public sealed class SignOnDbContext(DbContextOptions<SignOnDbContext> options) : DbContext(options)
{
    public DbSet<SecurityUser> Users => Set<SecurityUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecurityUser>(e =>
        {
            e.ToTable("usrsec");
            e.HasKey(u => u.UserId);
            e.Property(u => u.UserId).HasColumnName("sec_usr_id").HasColumnType("char(8)");
            e.Property(u => u.FirstName).HasColumnName("sec_usr_fname").HasColumnType("char(20)");
            e.Property(u => u.LastName).HasColumnName("sec_usr_lname").HasColumnType("char(20)");
            e.Property(u => u.Password).HasColumnName("sec_usr_pwd").HasColumnType("char(8)");
            e.Property(u => u.UserType).HasColumnName("sec_usr_type").HasColumnType("char(1)");
        });
    }
}

public interface ISecurityUserStore
{
    Task<SecurityUser?> FindAsync(string userId, CancellationToken ct);
}

/// <summary>Keyed read of USRSEC (COSGN00C READ-USER-SEC-FILE). CHAR padding is trimmed.</summary>
public sealed class SecurityUserStore(SignOnDbContext db) : ISecurityUserStore
{
    public async Task<SecurityUser?> FindAsync(string userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return null;
        return new SecurityUser
        {
            UserId = user.UserId.TrimEnd(),
            FirstName = user.FirstName.TrimEnd(),
            LastName = user.LastName.TrimEnd(),
            Password = user.Password.TrimEnd(),
            UserType = user.UserType.TrimEnd()
        };
    }
}
