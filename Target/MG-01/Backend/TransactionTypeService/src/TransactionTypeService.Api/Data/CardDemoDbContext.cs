using Microsoft.EntityFrameworkCore;
using TransactionTypeService.Api.Domain;

namespace TransactionTypeService.Api.Data;

/// <summary>EF Core context for the MG-01 PostgreSQL schema.</summary>
public class CardDemoDbContext(DbContextOptions<CardDemoDbContext> options) : DbContext(options)
{
    public DbSet<TransactionType> TransactionTypes => Set<TransactionType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.ToTable("transaction_type");
            entity.HasKey(t => t.TypeCode);
            entity.Property(t => t.TypeCode).HasColumnName("tr_type").HasColumnType("char(2)").IsFixedLength();
            entity.Property(t => t.Description).HasColumnName("tr_description").HasMaxLength(50).IsRequired();
        });
    }
}
