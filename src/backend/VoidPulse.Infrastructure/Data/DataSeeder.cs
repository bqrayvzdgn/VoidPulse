using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoidPulse.Application.Interfaces;
using VoidPulse.Domain.Entities;

namespace VoidPulse.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        // Only seed when the database has no tenants (first run)
        if (await dbContext.Tenants.AnyAsync())
        {
            logger.LogDebug("Database already seeded — skipping");
            return;
        }

        var seeding = configuration.GetSection("Seeding");
        var tenantName = seeding["TenantName"] ?? "Default";
        var tenantSlug = seeding["TenantSlug"] ?? "default";
        var adminEmail = seeding["AdminEmail"] ?? "admin@voidpulse.local";
        var adminPassword = seeding["AdminPassword"] ?? "ChangeMe123!";
        var adminFullName = seeding["AdminFullName"] ?? "System Administrator";
        var agentApiKey = seeding["AgentApiKey"];

        logger.LogInformation("First run detected — seeding default data...");

        // 1. Create default tenant
        var tenant = new Tenant
        {
            Name = tenantName,
            Slug = tenantSlug,
            IsActive = true
        };
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        // 2. Create admin user
        var user = new User
        {
            TenantId = tenant.Id,
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(adminPassword),
            FullName = adminFullName,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // 3. Assign TenantAdmin role
        var tenantAdminRole = await dbContext.Roles.FirstAsync(r => r.Name == "TenantAdmin");
        dbContext.Set<UserRole>().Add(new UserRole
        {
            UserId = user.Id,
            RoleId = tenantAdminRole.Id
        });
        await dbContext.SaveChangesAsync();

        // 4. Create agent key (if provided)
        if (!string.IsNullOrWhiteSpace(agentApiKey))
        {
            var agentKey = new AgentKey
            {
                TenantId = tenant.Id,
                Name = "Default Agent",
                ApiKey = agentApiKey,
                IsActive = true
            };
            dbContext.AgentKeys.Add(agentKey);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Agent key seeded: {KeyPrefix}...", agentApiKey[..Math.Min(10, agentApiKey.Length)]);
        }

        logger.LogInformation("Default data seeded — tenant={Tenant}, admin={Email}", tenantSlug, adminEmail);
    }
}
