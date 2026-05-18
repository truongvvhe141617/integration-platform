using BusinessService.Domain.Common;
using BusinessService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessService.Infrastructure.Persistence;

public class BusinessDbContext : DbContext
{
    public BusinessDbContext(DbContextOptions<BusinessDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(e =>
        {
            e.ToTable("biz_payment_transaction");
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderId).HasMaxLength(100).IsRequired();
            e.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.CorrelationId);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
