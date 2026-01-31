using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class AlertRuleRepository : Repository<AlertRule>, IAlertRuleRepository
{
    public AlertRuleRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AlertRule>> GetByTenantAsync(Guid tenantId)
    {
        return await DbSet
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AlertRule>> GetActiveByTenantAsync(Guid tenantId)
    {
        return await DbSet
            .Where(r => r.TenantId == tenantId && r.IsEnabled)
            .ToListAsync();
    }
}
