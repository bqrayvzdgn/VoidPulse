using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IDnsResolutionRepository : IRepository<DnsResolution>
{
    Task AddBatchAsync(IEnumerable<DnsResolution> resolutions);

    Task<List<(string Hostname, int QueryCount, DateTime LastSeen)>> GetTopQueriedHostnamesAsync(
        Guid tenantId, DateTime startDate, DateTime endDate, int limit = 20);

    Task<string?> ResolveIpToHostnameAsync(Guid tenantId, string ip);
}
