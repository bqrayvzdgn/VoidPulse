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

    Task<(long TotalFlows, long TotalBytes, int UniqueSourceIps, int UniqueDestIps)> GetOverviewStatsAsync(
        Guid tenantId, DateTime startDate, DateTime endDate);

    Task<List<(string Ip, long TotalBytes, int FlowCount)>> GetTopTalkersAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 10);

    Task<List<(string Protocol, long TotalBytes, int FlowCount)>> GetProtocolDistributionAsync(
        Guid tenantId, DateTime startDate, DateTime endDate);

    Task<List<(DateTime Hour, long BytesSent, long BytesReceived)>> GetBandwidthTimelineAsync(
        Guid tenantId, DateTime startDate, DateTime endDate);

    Task<int> DeleteOlderThanAsync(Guid tenantId, DateTime cutoff);

    Task<List<(string Hostname, long TotalBytes, int FlowCount, DateTime LastSeen)>> GetTopSitesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 20);

    Task<List<(string ProcessName, long TotalBytes, int FlowCount, DateTime LastSeen)>> GetTopProcessesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 20);

    Task<Guid?> FindFlowIdByTupleAsync(
        Guid tenantId, string sourceIp, string destIp, int sourcePort, int destPort, string protocol, DateTime capturedAt);
}
