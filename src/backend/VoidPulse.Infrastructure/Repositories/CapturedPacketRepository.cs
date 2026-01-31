using Microsoft.EntityFrameworkCore;
using VoidPulse.Domain.Entities;
using VoidPulse.Domain.Interfaces;
using VoidPulse.Infrastructure.Data;

namespace VoidPulse.Infrastructure.Repositories;

public class CapturedPacketRepository : Repository<CapturedPacket>, ICapturedPacketRepository
{
    public CapturedPacketRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<CapturedPacket> Items, int TotalCount)> GetByFlowIdAsync(
        Guid flowId, Guid tenantId, int page, int pageSize)
    {
        var query = DbSet
            .Where(cp => cp.TrafficFlowId == flowId && cp.TenantId == tenantId)
            .OrderBy(cp => cp.CapturedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<CapturedPacket> Items, int TotalCount)> QueryAsync(
        Guid tenantId,
        string? sourceIp,
        string? destIp,
        string? protocol,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        int page,
        int pageSize)
    {
        var query = DbSet
            .Where(cp => cp.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(sourceIp))
            query = query.Where(cp => cp.SourceIp == sourceIp);

        if (!string.IsNullOrWhiteSpace(destIp))
            query = query.Where(cp => cp.DestinationIp == destIp);

        if (!string.IsNullOrWhiteSpace(protocol))
            query = query.Where(cp => cp.Protocol == protocol);

        if (startDate.HasValue)
            query = query.Where(cp => cp.CapturedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(cp => cp.CapturedAt <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(cp => cp.Info.Contains(search));

        query = query.OrderByDescending(cp => cp.CapturedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddBatchAsync(IEnumerable<CapturedPacket> packets)
    {
        await DbSet.AddRangeAsync(packets);
        await Context.SaveChangesAsync();
    }

    public async Task<int> DeleteOlderThanAsync(Guid tenantId, DateTime cutoff)
    {
        return await DbSet
            .Where(cp => cp.TenantId == tenantId && cp.CapturedAt < cutoff)
            .ExecuteDeleteAsync();
    }
}
