using IntegrationService.Domain.Common;
using IntegrationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Infrastructure.Persistence;

/// <summary>
/// DB riêng của Integration Service — giống INContext trong iolis-instrument.
/// Chỉ chứa các bảng thuộc Integration Service.
/// </summary>
public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options) { }

    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<RetryLog> RetryLogs => Set<RetryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExecutionLog>(e =>
        {
            e.ToTable("execution_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.Property(x => x.ConfigKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.Operation).HasMaxLength(100).IsRequired();
            e.Property(x => x.RequestBody).HasColumnType("nvarchar(max)");
            e.Property(x => x.RequestHeaders).HasColumnType("nvarchar(max)");
            e.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
            e.HasIndex(x => x.CorrelationId);
            e.HasIndex(x => x.ConfigKey);
            e.HasIndex(x => x.StartedAt);

            e.HasMany(x => x.RetryLogs)
             .WithOne(x => x.ExecutionLog)
             .HasForeignKey(x => x.ExecutionLogId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RetryLog>(e =>
        {
            e.ToTable("retry_log");
            e.HasKey(x => x.Id);
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
