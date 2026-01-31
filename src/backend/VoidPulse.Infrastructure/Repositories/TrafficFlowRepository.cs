using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class TrafficFlowRepository : Repository<TrafficFlow>, ITrafficFlowRepository
{
    public TrafficFlowRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<TrafficFlow> Items, int TotalCount)> QueryAsync(
        Guid tenantId,
        string? sourceIp,
        string? destIp,
        string? protocol,
        DateTime? startDate,
        DateTime? endDate,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize)
    {
        var query = DbSet
            .Where(tf => tf.TenantId == tenantId)
            .Include(tf => tf.HttpMetadata)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(sourceIp))
            query = query.Where(tf => tf.SourceIp == sourceIp);

        if (!string.IsNullOrWhiteSpace(destIp))
            query = query.Where(tf => tf.DestinationIp == destIp);

        if (!string.IsNullOrWhiteSpace(protocol))
            query = query.Where(tf => tf.Protocol == protocol);

        if (startDate.HasValue)
            query = query.Where(tf => tf.StartedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(tf => tf.EndedAt <= endDate.Value);

        // Apply sorting
        var isAscending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        query = sortBy?.ToLowerInvariant() switch
        {
            "sourceip" => isAscending
                ? query.OrderBy(tf => tf.SourceIp)
                : query.OrderByDescending(tf => tf.SourceIp),
            "destinationip" => isAscending
                ? query.OrderBy(tf => tf.DestinationIp)
                : query.OrderByDescending(tf => tf.DestinationIp),
            "bytessent" => isAscending
                ? query.OrderBy(tf => tf.BytesSent)
                : query.OrderByDescending(tf => tf.BytesSent),
            "startedat" => isAscending
                ? query.OrderBy(tf => tf.StartedAt)
                : query.OrderByDescending(tf => tf.StartedAt),
            _ => query.OrderByDescending(tf => tf.StartedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddBatchAsync(IEnumerable<TrafficFlow> flows)
    {
        DbSet.AddRange(flows);
        await Context.SaveChangesAsync();
    }

    public async Task<(long TotalFlows, long TotalBytes, int UniqueSourceIps, int UniqueDestIps)> GetOverviewStatsAsync(
        Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var stats = await DbSet
            .Where(tf => tf.TenantId == tenantId && tf.StartedAt >= startDate && tf.EndedAt <= endDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalFlows = g.LongCount(),
                TotalBytes = g.Sum(f => f.BytesSent + f.BytesReceived),
                UniqueSourceIps = g.Select(f => f.SourceIp).Distinct().Count(),
                UniqueDestIps = g.Select(f => f.DestinationIp).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        if (stats is null)
            return (0, 0, 0, 0);

        return (stats.TotalFlows, stats.TotalBytes, stats.UniqueSourceIps, stats.UniqueDestIps);
    }

    public async Task<List<(string Ip, long TotalBytes, int FlowCount)>> GetTopTalkersAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 10)
    {
        var results = await DbSet
            .Where(tf => tf.TenantId == tenantId && tf.StartedAt >= startDate && tf.EndedAt <= endDate)
            .GroupBy(tf => tf.SourceIp)
            .Select(g => new
            {
                Ip = g.Key,
                TotalBytes = g.Sum(f => f.BytesSent + f.BytesReceived),
                FlowCount = g.Count()
            })
            .OrderByDescending(x => x.TotalBytes)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => (r.Ip, r.TotalBytes, r.FlowCount)).ToList();
    }

    public async Task<List<(string Protocol, long TotalBytes, int FlowCount)>> GetProtocolDistributionAsync(
        Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var results = await DbSet
            .Where(tf => tf.TenantId == tenantId && tf.StartedAt >= startDate && tf.EndedAt <= endDate)
            .GroupBy(tf => tf.Protocol)
            .Select(g => new
            {
                Protocol = g.Key,
                TotalBytes = g.Sum(f => f.BytesSent + f.BytesReceived),
                FlowCount = g.Count()
            })
            .OrderByDescending(x => x.TotalBytes)
            .ToListAsync();

        return results.Select(r => (r.Protocol, r.TotalBytes, r.FlowCount)).ToList();
    }

    public async Task<List<(DateTime Hour, long BytesSent, long BytesReceived)>> GetBandwidthTimelineAsync(
        Guid tenantId, DateTime startDate, DateTime endDate)
    {
        var results = await DbSet
            .Where(tf => tf.TenantId == tenantId && tf.StartedAt >= startDate && tf.EndedAt <= endDate)
            .GroupBy(tf => new { tf.StartedAt.Year, tf.StartedAt.Month, tf.StartedAt.Day, tf.StartedAt.Hour })
            .Select(g => new
            {
                Hour = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                BytesSent = g.Sum(f => f.BytesSent),
                BytesReceived = g.Sum(f => f.BytesReceived)
            })
            .OrderBy(x => x.Hour)
            .ToListAsync();

        return results.Select(r => (r.Hour, r.BytesSent, r.BytesReceived)).ToList();
    }

    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTime cutoff)
    {
        return await DbSet
            .Where(tf => tf.TenantId == tenantId && tf.EndedAt < cutoff)
            .ExecuteDeleteAsync();
    }

    public async Task<List<(string Hostname, long TotalBytes, int FlowCount, DateTime LastSeen)>> GetTopSitesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 20)
    {
        // Use ResolvedHostname, falling back to TlsSni for sites without DNS resolution
        var results = await DbSet
            .Where(tf => tf.TenantId == tenantId
                && tf.StartedAt >= startDate
                && tf.EndedAt <= endDate
                && ((tf.ResolvedHostname != null && tf.ResolvedHostname != "")
                    || (tf.TlsSni != null && tf.TlsSni != "")))
            .Select(tf => new
            {
                tf.BytesSent,
                tf.BytesReceived,
                tf.EndedAt,
                EffectiveHostname = (tf.ResolvedHostname != null && tf.ResolvedHostname != "")
                    ? tf.ResolvedHostname
                    : tf.TlsSni!
            })
            .GroupBy(x => x.EffectiveHostname)
            .Select(g => new
            {
                Hostname = g.Key,
                TotalBytes = g.Sum(f => f.BytesSent + f.BytesReceived),
                FlowCount = g.Count(),
                LastSeen = g.Max(f => f.EndedAt)
            })
            .OrderByDescending(x => x.TotalBytes)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => (r.Hostname, r.TotalBytes, r.FlowCount, r.LastSeen)).ToList();
    }

    public async Task<List<(string ProcessName, long TotalBytes, int FlowCount, DateTime LastSeen)>> GetTopProcessesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 20)
    {
        var results = await DbSet
            .Where(tf => tf.TenantId == tenantId
                && tf.StartedAt >= startDate
                && tf.EndedAt <= endDate
                && tf.ProcessName != null
                && tf.ProcessName != "")
            .GroupBy(tf => tf.ProcessName!)
            .Select(g => new
            {
                ProcessName = g.Key,
                TotalBytes = g.Sum(f => f.BytesSent + f.BytesReceived),
                FlowCount = g.Count(),
                LastSeen = g.Max(f => f.EndedAt)
            })
            .OrderByDescending(x => x.TotalBytes)
            .Take(limit)
            .ToListAsync();

        return results.Select(r => (r.ProcessName, r.TotalBytes, r.FlowCount, r.LastSeen)).ToList();
    }

    public async Task<Guid?> FindFlowIdByTupleAsync(
        Guid tenantId, string sourceIp, string destIp, int sourcePort, int destPort, string protocol, DateTime capturedAt)
    {
        // Find the most recent flow matching this 5-tuple within a reasonable time window
        var windowStart = capturedAt.AddMinutes(-5);
        var windowEnd = capturedAt.AddMinutes(5);

        var flow = await DbSet
            .Where(tf => tf.TenantId == tenantId
                && tf.Protocol == protocol
                && tf.StartedAt <= windowEnd
                && tf.EndedAt >= windowStart
                && ((tf.SourceIp == sourceIp && tf.DestinationIp == destIp
                    && tf.SourcePort == sourcePort && tf.DestinationPort == destPort)
                    || (tf.SourceIp == destIp && tf.DestinationIp == sourceIp
                    && tf.SourcePort == destPort && tf.DestinationPort == sourcePort)))
            .OrderByDescending(tf => tf.EndedAt)
            .Select(tf => (Guid?)tf.Id)
            .FirstOrDefaultAsync();

        return flow;
    }
}
