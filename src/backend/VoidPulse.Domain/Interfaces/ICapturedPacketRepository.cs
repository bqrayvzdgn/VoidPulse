using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface ICapturedPacketRepository : IRepository<CapturedPacket>
{
    Task<(IReadOnlyList<CapturedPacket> Items, int TotalCount)> GetByFlowIdAsync(
        Guid flowId, Guid tenantId, int page, int pageSize);

    Task<(IReadOnlyList<CapturedPacket> Items, int TotalCount)> QueryAsync(
        Guid tenantId,
        string? sourceIp,
        string? destIp,
        string? protocol,
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        int page,
        int pageSize);

    Task AddBatchAsync(IEnumerable<CapturedPacket> packets);

    Task<int> DeleteOlderThanAsync(Guid tenantId, DateTime cutoff);
}
