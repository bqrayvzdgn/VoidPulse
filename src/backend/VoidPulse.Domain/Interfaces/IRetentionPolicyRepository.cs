using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IRetentionPolicyRepository : IRepository<RetentionPolicy>
{
    Task<RetentionPolicy?> GetByTenantAsync(Guid tenantId);
}
