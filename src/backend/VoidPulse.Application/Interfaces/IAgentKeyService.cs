using VoidPulse.Application.DTOs.Agents;

namespace VoidPulse.Application.Interfaces;

public interface IAgentKeyService
{
    Task<AgentKeyResponse> GetByIdAsync(Guid id, Guid tenantId);
    Task<IReadOnlyList<AgentKeyResponse>> GetByTenantAsync(Guid tenantId);
    Task<AgentKeyResponse> CreateAsync(Guid tenantId, CreateAgentKeyRequest request);
    Task<AgentKeyResponse> UpdateAsync(Guid id, Guid tenantId, UpdateAgentKeyRequest request);
    Task DeleteAsync(Guid id, Guid tenantId);
}
