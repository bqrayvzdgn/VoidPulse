using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IAlertRuleRepository : IRepository<AlertRule>
{
    Task<IReadOnlyList<AlertRule>> GetByTenantAsync(Guid tenantId);
    Task<IReadOnlyList<AlertRule>> GetActiveByTenantAsync(Guid tenantId);
}
