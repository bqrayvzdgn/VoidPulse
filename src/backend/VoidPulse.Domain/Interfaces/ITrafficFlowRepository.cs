using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface ITrafficFlowRepository : IRepository<TrafficFlow>
{
    Task<(IReadOnlyList<TrafficFlow> Items, int TotalCount)> QueryAsync(
        Guid tenantId,
        string? sourceIp,
        string? destIp,
        string? protocol,
        DateTime? startDate,
        DateTime? endDate,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize);

    Task AddBatchAsync(IEnumerable<TrafficFlow> flows);
}
