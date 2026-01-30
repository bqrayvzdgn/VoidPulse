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
}
