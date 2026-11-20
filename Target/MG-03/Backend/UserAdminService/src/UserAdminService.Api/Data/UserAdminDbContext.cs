using Microsoft.EntityFrameworkCore;

namespace UserAdminService.Api.Data;

public sealed class UserAdminDbContext(DbContextOptions<UserAdminDbContext> options) : DbContext(options)
{
    public DbSet<SecurityUser> Users => Set<SecurityUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<SecurityUser>();
        e.ToTable("usrsec");
        e.HasKey(u => u.UserId).HasName("usrsec_pk");
        e.Property(u => u.UserId).HasColumnName("sec_usr_id").HasColumnType("char(8)");
        e.Property(u => u.FirstName).HasColumnName("sec_usr_fname").HasColumnType("char(20)");
        e.Property(u => u.LastName).HasColumnName("sec_usr_lname").HasColumnType("char(20)");
        e.Property(u => u.Password).HasColumnName("sec_usr_pwd").HasColumnType("char(8)");
        e.Property(u => u.UserType).HasColumnName("sec_usr_type").HasColumnType("char(1)");
        e.Property(u => u.Version).IsRowVersion();
    }
}
