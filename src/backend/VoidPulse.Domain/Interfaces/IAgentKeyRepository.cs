using VoidPulse.Domain.Entities;

namespace VoidPulse.Domain.Interfaces;

public interface IAgentKeyRepository : IRepository<AgentKey>
{
    Task<AgentKey?> GetByApiKeyAsync(string apiKey);
    Task<IReadOnlyList<AgentKey>> GetByTenantAsync(Guid tenantId);
}
