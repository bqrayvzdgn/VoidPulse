using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class AlertRepository : Repository<Alert>, IAlertRepository
{
    public AlertRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Alert> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, bool? isAcknowledged, AlertSeverity? severity, int page, int pageSize)
    {
        var query = DbSet
            .Where(a => a.TenantId == tenantId)
            .Include(a => a.AlertRule)
            .AsQueryable();

        if (isAcknowledged.HasValue)
            query = query.Where(a => a.IsAcknowledged == isAcknowledged.Value);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<int> GetUnacknowledgedCountAsync(Guid tenantId)
    {
        return await DbSet.CountAsync(a => a.TenantId == tenantId && !a.IsAcknowledged);
    }

    public async Task AddBatchAsync(IEnumerable<Alert> alerts)
    {
        DbSet.AddRange(alerts);
        await Context.SaveChangesAsync();
    }

    public async Task<int> GetRecentPortCountAsync(Guid tenantId, string sourceIp, DateTime since)
    {
        // Count distinct destination ports from recent traffic for port scan detection
        return await Context.Set<TrafficFlow>()
            .Where(tf => tf.TenantId == tenantId && tf.SourceIp == sourceIp && tf.StartedAt >= since)
            .Select(tf => tf.DestinationPort)
            .Distinct()
            .CountAsync();
    }
}
