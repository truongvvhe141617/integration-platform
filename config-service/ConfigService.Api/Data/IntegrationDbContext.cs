using ConfigService.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConfigService.Api.Data;

public class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options) : base(options) { }

    public DbSet<IntegrationConfigEntity> Configs => Set<IntegrationConfigEntity>();
    public DbSet<IntegrationOperationEntity> Operations => Set<IntegrationOperationEntity>();
    public DbSet<RequestMappingEntity> RequestMappings => Set<RequestMappingEntity>();
    public DbSet<ResponseMappingEntity> ResponseMappings => Set<ResponseMappingEntity>();
    public DbSet<ValidationRuleEntity> ValidationRules => Set<ValidationRuleEntity>();
    public DbSet<ConfigHistoryEntity> ConfigHistory => Set<ConfigHistoryEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationConfigEntity>(e =>
        {
            e.HasIndex(x => x.ConfigId).IsUnique().HasFilter("[IsDeleted] = 0");
            e.HasIndex(x => x.Status).HasFilter("[IsDeleted] = 0");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<IntegrationOperationEntity>(e =>
        {
            e.HasOne(x => x.Config).WithMany(x => x.Operations).HasForeignKey(x => x.ConfigId);
            e.HasIndex(x => new { x.ConfigId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<RequestMappingEntity>(e =>
        {
            e.HasOne(x => x.Operation).WithMany(x => x.RequestMappings).HasForeignKey(x => x.OperationId);
        });

        modelBuilder.Entity<ResponseMappingEntity>(e =>
        {
            e.HasOne(x => x.Operation).WithMany(x => x.ResponseMappings).HasForeignKey(x => x.OperationId);
        });

        modelBuilder.Entity<ValidationRuleEntity>(e =>
        {
            e.HasOne(x => x.Operation).WithMany(x => x.Validations).HasForeignKey(x => x.OperationId);
        });

        modelBuilder.Entity<ConfigHistoryEntity>(e =>
        {
            e.HasIndex(x => new { x.ConfigId, x.Version }).IsDescending(false, true);
        });

        modelBuilder.Entity<AuditLogEntity>(e =>
        {
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.PerformedAt).IsDescending();
        });
    }
}
