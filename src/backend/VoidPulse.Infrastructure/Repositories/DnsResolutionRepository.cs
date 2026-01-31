using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class DnsResolutionRepository : Repository<DnsResolution>, IDnsResolutionRepository
{
    public DnsResolutionRepository(AppDbContext context) : base(context)
    {
    }

    public async Task AddBatchAsync(IEnumerable<DnsResolution> resolutions)
    {
        DbSet.AddRange(resolutions);
        await Context.SaveChangesAsync();
    }

    public async Task<List<(string Hostname, int QueryCount, DateTime LastSeen)>> GetTopQueriedHostnamesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 20)
    {
        var results = await DbSet
            .Where(d => d.TenantId == tenantId && d.ResolvedAt >= startDate && d.ResolvedAt <= endDate)
            .GroupBy(d => d.QueriedHostname)
            .Select(g => new
            {
                Hostname = g.Key,
                QueryCount = g.Count(),
                LastSeen = g.Max(d => d.ResolvedAt)
            })
            .OrderByDescending(x => x.QueryCount)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => (r.Hostname, r.QueryCount, r.LastSeen)).ToList();
    }

    public async Task<string?> ResolveIpToHostnameAsync(Guid tenantId, string ip)
    {
        return await DbSet
            .Where(d => d.TenantId == tenantId && d.ResolvedIp == ip)
            .OrderByDescending(d => d.ResolvedAt)
            .Select(d => d.QueriedHostname)
            .FirstOrDefaultAsync();
    }
}
