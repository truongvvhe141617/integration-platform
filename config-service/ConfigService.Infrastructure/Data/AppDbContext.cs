using ConfigService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConfigService.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<IntegrationConfig> IntegrationConfigs => Set<IntegrationConfig>();
    public DbSet<IntegrationOperation> IntegrationOperations => Set<IntegrationOperation>();
    public DbSet<RequestMapping> RequestMappings => Set<RequestMapping>();
    public DbSet<ResponseMapping> ResponseMappings => Set<ResponseMapping>();
    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IntegrationConfig
        modelBuilder.Entity<IntegrationConfig>(e =>
        {
            e.ToTable("integration_config");
            e.HasKey(x => x.Id);
            e.Property(x => x.ConfigKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();
            e.Property(x => x.BaseUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.ConnectorType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.AuthType).HasMaxLength(50).IsRequired();
            e.Property(x => x.AuthParams).HasColumnType("nvarchar(max)");
            e.Property(x => x.DefaultHeaders).HasColumnType("nvarchar(max)");
            e.Property(x => x.RetryConfig).HasColumnType("nvarchar(max)");
            e.Property(x => x.CircuitBreaker).HasColumnType("nvarchar(max)");
            e.Property(x => x.Metadata).HasColumnType("nvarchar(max)");
            e.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(100);
            // Query filter disabled - filter manually in repository
            // e.HasQueryFilter(x => x.IsDeleted == false);
            e.HasIndex(x => new { x.TenantId, x.ConfigKey }).IsUnique();

            e.HasMany(x => x.Operations)
             .WithOne()
             .HasForeignKey(x => x.ConfigId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // IntegrationOperation
        modelBuilder.Entity<IntegrationOperation>(e =>
        {
            e.ToTable("integration_operation");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
            e.Property(x => x.Path).HasMaxLength(500).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.HasIndex(x => new { x.ConfigId, x.Name }).IsUnique();

            e.HasMany(x => x.RequestMappings)
             .WithOne()
             .HasForeignKey(x => x.OperationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.ResponseMappings)
             .WithOne()
             .HasForeignKey(x => x.OperationId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.ValidationRules)
             .WithOne()
             .HasForeignKey(x => x.OperationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RequestMapping>(e =>
        {
            e.ToTable("request_mapping");
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceField).HasMaxLength(200).IsRequired();
            e.Property(x => x.TargetField).HasMaxLength(200).IsRequired();
            e.Property(x => x.DefaultValue).HasMaxLength(500);
            e.Property(x => x.Transform).HasMaxLength(100);
        });

        modelBuilder.Entity<ResponseMapping>(e =>
        {
            e.ToTable("response_mapping");
            e.HasKey(x => x.Id);
            e.Property(x => x.SourceField).HasMaxLength(200).IsRequired();
            e.Property(x => x.TargetField).HasMaxLength(200).IsRequired();
            e.Property(x => x.DefaultValue).HasMaxLength(500);
            e.Property(x => x.Transform).HasMaxLength(100);
        });

        modelBuilder.Entity<ValidationRule>(e =>
        {
            e.ToTable("validation_rule");
            e.HasKey(x => x.Id);
            e.Property(x => x.Field).HasMaxLength(200).IsRequired();
            e.Property(x => x.Rule).HasMaxLength(200).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
        });
    }
}
