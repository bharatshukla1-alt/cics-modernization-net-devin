using Microsoft.EntityFrameworkCore;
using TransactionTypeService.Api.Models;

namespace TransactionTypeService.Api.Repositories;

public class TransactionTypeContext : DbContext
{
    public TransactionTypeContext(DbContextOptions<TransactionTypeContext> options) : base(options) { }

    public DbSet<TransactionType> TransactionTypes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.ToTable("transaction_type");
            entity.HasKey(e => e.TypeCode);
            entity.Property(e => e.TypeCode).HasColumnName("tr_type").HasMaxLength(2).IsFixedLength();
            entity.Property(e => e.Description).HasColumnName("tr_description").HasMaxLength(50).IsRequired();
        });
    }
}
