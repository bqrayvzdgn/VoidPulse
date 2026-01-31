using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IAlertRepository : IRepository<Alert>
{
    Task<(IReadOnlyList<Alert> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, bool? isAcknowledged, AlertSeverity? severity, int page, int pageSize);
    Task<int> GetUnacknowledgedCountAsync(Guid tenantId);
    Task AddBatchAsync(IEnumerable<Alert> alerts);
    Task<int> GetRecentPortCountAsync(Guid tenantId, string sourceIp, DateTime since);
}
