using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private Guid? _currentTenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<TrafficFlow> TrafficFlows => Set<TrafficFlow>();
    public DbSet<HttpMetadata> HttpMetadata => Set<HttpMetadata>();
    public DbSet<AgentKey> AgentKeys => Set<AgentKey>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<SavedFilter> SavedFilters => Set<SavedFilter>();
    public DbSet<DnsResolution> DnsResolutions => Set<DnsResolution>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<CapturedPacket> CapturedPackets => Set<CapturedPacket>();

    public void SetTenantId(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter for soft-delete on all BaseEntity-derived types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { modelBuilder });
            }
        }

        // Seed default roles
        var superAdminId = new Guid("a1b2c3d4-0001-0001-0001-000000000001");
        var tenantAdminId = new Guid("a1b2c3d4-0001-0001-0001-000000000002");
        var analystId = new Guid("a1b2c3d4-0001-0001-0001-000000000003");
        var viewerId = new Guid("a1b2c3d4-0001-0001-0001-000000000004");

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = superAdminId, Name = "SuperAdmin", Description = "Full system access across all tenants", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = tenantAdminId, Name = "TenantAdmin", Description = "Full access within a tenant", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = analystId, Name = "Analyst", Description = "Read and analyze traffic data", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = viewerId, Name = "Viewer", Description = "Read-only access to dashboards", CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }

    private static void ApplySoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => e.DeletedAt == null);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
